// Copyright (c) Microsoft. All rights reserved.

using Azure.Core;
using Microsoft.AspNetCore.SignalR;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;
using MinimalApi.Hubs;
using System.Text;
using Microsoft.Extensions.Logging;

namespace MinimalApi.Services;
#pragma warning disable SKEXP0011 // Mark members as static
#pragma warning disable SKEXP0001 // Mark members as static
public class ReadRetrieveReadChatService
{
    private readonly ISearchService _searchClient;
    private readonly Kernel _kernel;
    private readonly IConfiguration _configuration;
    private readonly IComputerVisionService? _visionService;
    private readonly TokenCredential? _tokenCredential;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ILogger<ReadRetrieveReadChatService> _logger;

    private record StreamingMessage<T>(string Type, T Content);

    public ReadRetrieveReadChatService(
        ISearchService searchClient,
        OpenAIClient client,
        IConfiguration configuration,
        IHubContext<ChatHub> hubContext,
        IComputerVisionService? visionService = null,
        TokenCredential? tokenCredential = null,
        ILogger<ReadRetrieveReadChatService> logger = default!)
    {
        _searchClient = searchClient;
        var kernelBuilder = Kernel.CreateBuilder();

        if (configuration["UseAOAI"] == "false")
        {
            var deployment = configuration["OpenAiChatGptDeployment"];
            ArgumentNullException.ThrowIfNullOrWhiteSpace(deployment);
            kernelBuilder = kernelBuilder.AddOpenAIChatCompletion(deployment, client);

            var embeddingModelName = configuration["OpenAiEmbeddingDeployment"];
            ArgumentNullException.ThrowIfNullOrWhiteSpace(embeddingModelName);
            kernelBuilder = kernelBuilder.AddOpenAITextEmbeddingGeneration(embeddingModelName, client);
        }
        else
        {
            var deployedModelName = configuration["AzureOpenAiChatGptDeployment"];
            ArgumentNullException.ThrowIfNullOrWhiteSpace(deployedModelName);
            var embeddingModelName = configuration["AzureOpenAiEmbeddingDeployment"];
            if (!string.IsNullOrEmpty(embeddingModelName))
            {
                var endpoint = configuration["AzureOpenAiServiceEndpoint"];
                ArgumentNullException.ThrowIfNullOrWhiteSpace(endpoint);
                kernelBuilder = kernelBuilder.AddAzureOpenAITextEmbeddingGeneration(embeddingModelName, endpoint, tokenCredential ?? new DefaultAzureCredential());
                kernelBuilder = kernelBuilder.AddAzureOpenAIChatCompletion(deployedModelName, endpoint, tokenCredential ?? new DefaultAzureCredential());
            }
        }

        _kernel = kernelBuilder.Build();
        _configuration = configuration;
        _visionService = visionService;
        _tokenCredential = tokenCredential;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task<ChatAppResponse> ReplyAsync(
        ChatMessage[] history,
        RequestOverrides? overrides,
        string? connectionId = null,
        CancellationToken cancellationToken = default)
    {
        var top = overrides?.Top ?? 3;
        var useSemanticCaptions = overrides?.SemanticCaptions ?? false;
        var useSemanticRanker = overrides?.SemanticRanker ?? false;
        var excludeCategory = overrides?.ExcludeCategory ?? null;
        var filter = excludeCategory is null ? null : $"category ne '{excludeCategory}'";
        var chat = _kernel.GetRequiredService<IChatCompletionService>();
        var embedding = _kernel.GetRequiredService<ITextEmbeddingGenerationService>();
        float[]? embeddings = null;
        var question = history.LastOrDefault(m => m.IsUser)?.Content is { } userQuestion
            ? userQuestion
            : throw new InvalidOperationException("Use question is null");

        string[]? followUpQuestionList = null;
        if (overrides?.RetrievalMode != RetrievalMode.Text && embedding is not null)
        {
            embeddings = (await embedding.GenerateEmbeddingAsync(question, cancellationToken: cancellationToken)).ToArray();
        }

        // step 1
        // use llm to get query if retrieval mode is not vector
        string? query = null;
        if (overrides?.RetrievalMode != RetrievalMode.Vector)
        {
            var getQueryChat = new ChatHistory(@"You are a helpful AI assistant, generate search query for followup question.
Make your respond simple and precise. Return the query only, do not return any other text.
e.g.
Northwind Health Plus AND standard plan.
standard plan AND dental AND employee benefit.
");

            getQueryChat.AddUserMessage(question);
            var result = await chat.GetChatMessageContentAsync(
                getQueryChat,
                cancellationToken: cancellationToken);

            query = result.Content ?? throw new InvalidOperationException("Failed to get search query");
        }

        // step 2
        // use query to search related docs
        var documentContentList = await _searchClient.QueryDocumentsAsync(query, embeddings, overrides, cancellationToken);

        string documentContents = string.Empty;
        if (documentContentList.Length == 0)
        {
            documentContents = "no source available.";
        }
        else
        {
            documentContents = string.Join("\r", documentContentList.Select(x => $"{x.Title}:{x.Content}"));
        }

        // step 2.5
        // retrieve images if _visionService is available
        SupportingImageRecord[]? images = default;
        if (_visionService is not null)
        {
            var queryEmbeddings = await _visionService.VectorizeTextAsync(query ?? question, cancellationToken);
            images = await _searchClient.QueryImagesAsync(query, queryEmbeddings.vector, overrides, cancellationToken);
        }

        // step 3
        // put together related docs and conversation history to generate answer
        var answerChat = new ChatHistory(
            "You are a system assistant who helps the company employees with their questions. Be brief in your answers");

        // add chat history
        foreach (var message in history)
        {
            if (message.IsUser)
            {
                answerChat.AddUserMessage(message.Content);
            }
            else
            {
                answerChat.AddAssistantMessage(message.Content);
            }
        }


        if (images != null)
        {
            var prompt = @$"## Source ##
{documentContents}
## End ##

Answer question based on available source and images.
Your answer needs to be a json object with answer and thoughts field.
Don't put your answer between ```json and ```, return the json string directly. e.g {{""answer"": ""I don't know"", ""thoughts"": ""I don't know""}}";

            var tokenRequestContext = new TokenRequestContext(new[] { "https://storage.azure.com/.default" });
            var sasToken = await (_tokenCredential?.GetTokenAsync(tokenRequestContext, cancellationToken) ?? throw new InvalidOperationException("Failed to get token"));
            var sasTokenString = sasToken.Token;
            var imageUrls = images.Select(x => $"{x.Url}?{sasTokenString}").ToArray();
            var collection = new ChatMessageContentItemCollection();
            collection.Add(new TextContent(prompt));
            foreach (var imageUrl in imageUrls)
            {
                collection.Add(new ImageContent(new Uri(imageUrl)));
            }

            answerChat.AddUserMessage(collection);
        }
        else
        {
            var prompt = @$" ## Source ##
{documentContents}
## End ##

You answer needs to be a json object with the following format.
{{
    ""answer"": // the answer to the question, add a source reference to the end of each sentence. e.g. Apple is a fruit [reference1.pdf][reference2.pdf]. If no source available, put the answer as I don't know.
    ""thoughts"": // brief thoughts on how you came up with the answer, e.g. what sources you used, what you thought about, etc.
}}";
            answerChat.AddUserMessage(prompt);
        }

        var promptExecutingSetting = new OpenAIPromptExecutionSettings
        {
            MaxTokens = 1024,
            Temperature = overrides?.Temperature ?? 0.7,
            StopSequences = [],
        };

        // get answer
        var streamedAnswer = new StringBuilder();
        string ans;
        string thoughts;

        if (overrides?.UseStreaming == true && !string.IsNullOrEmpty(connectionId))
        {
            try
            {
                var streamingResponse = chat.GetStreamingChatMessageContentsAsync(
                    answerChat,
                    promptExecutingSetting,
                    cancellationToken: cancellationToken);

                var currentStreamedContent = new StringBuilder();
                await foreach (var content in streamingResponse)
                {
                    if (!string.IsNullOrEmpty(content.Content))
                    {
                        try
                        {
                            currentStreamedContent.Append(content.Content);

                            // Send raw content as streaming message
                            var streamingMessage = new StreamingMessage<string>("content", content.Content);
                            await _hubContext.Clients.Client(connectionId)
                                .SendAsync("ReceiveMessage", JsonSerializer.Serialize(streamingMessage), cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error sending message through SignalR");
                        }
                    }
                }

                // Parse the complete response after streaming
                var answerJson = currentStreamedContent.ToString();
                try
                {
                    var answerObject = JsonSerializer.Deserialize<JsonElement>(answerJson);
                    ans = answerObject.GetProperty("answer").GetString() ??
                        throw new InvalidOperationException("Failed to get answer");
                    thoughts = answerObject.GetProperty("thoughts").GetString() ??
                        throw new InvalidOperationException("Failed to get thoughts");

                    // Send thoughts as a separate message
                    var thoughtsMessage = new StreamingMessage<Thoughts[]>(
                        "thoughts",
                        new[] { new Thoughts("Thoughts", thoughts) });
                    await _hubContext.Clients.Client(connectionId)
                        .SendAsync("ReceiveMessage", JsonSerializer.Serialize(thoughtsMessage), cancellationToken);

                    // Handle follow-up questions
                    if (overrides?.SuggestFollowupQuestions is true)
                    {
                        var followUpQuestions = await GetFollowUpQuestionsAsync(
                            chat, ans, promptExecutingSetting, cancellationToken);

                        // Send follow-up questions as a separate message
                        var followUpMessage = new StreamingMessage<string[]>("followup", followUpQuestions);
                        await _hubContext.Clients.Client(connectionId)
                            .SendAsync("ReceiveMessage", JsonSerializer.Serialize(followUpMessage), cancellationToken);

                        foreach (var followupQuestion in followUpQuestions)
                        {
                            ans += $" <<{followupQuestion}>> ";
                        }
                    }

                    // Send supporting content
                    var supportingContent = new StreamingMessage<SupportingContentRecord[]>(
                        "supporting",
                        documentContentList.Select(x => new SupportingContentRecord(x.Title, x.Content)).ToArray());
                    await _hubContext.Clients.Client(connectionId)
                        .SendAsync("ReceiveMessage", JsonSerializer.Serialize(supportingContent), cancellationToken);

                    // Send supporting images if available
                    if (images?.Length > 0)
                    {
                        var supportingImages = new StreamingMessage<SupportingImageRecord[]>(
                            "images",
                            images.Select(x => new SupportingImageRecord(x.Title, x.Url)).ToArray());
                        await _hubContext.Clients.Client(connectionId)
                            .SendAsync("ReceiveMessage", JsonSerializer.Serialize(supportingImages), cancellationToken);
                    }

                    // Send final complete response
                    var finalResponse = new ChatAppResponse(new[] {
                        new ResponseChoice(
                            Index: 0,
                            Message: new ResponseMessage("assistant", ans),
                            Context: new ResponseContext(
                                DataPointsContent: documentContentList.Select(x => new SupportingContentRecord(x.Title, x.Content)).ToArray(),
                                DataPointsImages: images?.Select(x => new SupportingImageRecord(x.Title, x.Url)).ToArray(),
                                FollowupQuestions: followUpQuestionList ?? Array.Empty<string>(),
                                Thoughts: new[] { new Thoughts("Thoughts", thoughts) }),
                            CitationBaseUrl: _configuration.ToCitationBaseUrl())
                    });

                    var completeMessage = new StreamingMessage<ChatAppResponse>("complete", finalResponse);
                    await _hubContext.Clients.Client(connectionId)
                        .SendAsync("ReceiveMessage", JsonSerializer.Serialize(completeMessage), cancellationToken);
                }
                catch (JsonException)
                {
                    // Handle invalid JSON...
                    ans = answerJson;
                    thoughts = "Generated through streaming";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in streaming response");
                throw;
            }
        }
        else
        {
            var answer = await chat.GetChatMessageContentAsync(
                answerChat,
                promptExecutingSetting,
                cancellationToken: cancellationToken);
            var answerJson = answer.Content ??
                throw new InvalidOperationException("Failed to get response");
            var answerObject = JsonSerializer.Deserialize<JsonElement>(answerJson);
            ans = answerObject.GetProperty("answer").GetString() ??
                throw new InvalidOperationException("Failed to get answer");
            thoughts = answerObject.GetProperty("thoughts").GetString() ??
                throw new InvalidOperationException("Failed to get thoughts");
        }

        // step 4
        // add follow up questions if requested
        if (overrides?.SuggestFollowupQuestions is true)
        {
            var followUpQuestionChat = new ChatHistory(@"You are a helpful AI assistant");
            followUpQuestionChat.AddUserMessage($@"Generate three follow-up question based on the answer you just generated.
# Answer
{ans}

# Format of the response
Return the follow-up question as a json string list. Don't put your answer between ```json and ```, return the json string directly.
e.g.
[
    ""What is the deductible?"",
    ""What is the co-pay?"",
    ""What is the out-of-pocket maximum?""
]");

            var followUpQuestions = await chat.GetChatMessageContentAsync(
                followUpQuestionChat,
                promptExecutingSetting,
                cancellationToken: cancellationToken);

            var followUpQuestionsJson = followUpQuestions.Content ?? throw new InvalidOperationException("Failed to get search query");
            var followUpQuestionsObject = JsonSerializer.Deserialize<JsonElement>(followUpQuestionsJson);
            var followUpQuestionsList = followUpQuestionsObject.EnumerateArray().Select(x => x.GetString()!).ToList();
            foreach (var followUpQuestion in followUpQuestionsList)
            {
                ans += $" <<{followUpQuestion}>> ";
            }

            followUpQuestionList = followUpQuestionsList.ToArray();
        }

        var responseMessage = new ResponseMessage("assistant", ans);
        var responseContext = new ResponseContext(
            DataPointsContent: documentContentList.Select(x => new SupportingContentRecord(x.Title, x.Content)).ToArray(),
            DataPointsImages: images?.Select(x => new SupportingImageRecord(x.Title, x.Url)).ToArray(),
            FollowupQuestions: followUpQuestionList ?? Array.Empty<string>(),
            Thoughts: new[] { new Thoughts("Thoughts", thoughts) });

        var choice = new ResponseChoice(
            Index: 0,
            Message: responseMessage,
            Context: responseContext,
            CitationBaseUrl: _configuration.ToCitationBaseUrl());

        return new ChatAppResponse(new[] { choice });
    }

    private async Task<string[]> GetFollowUpQuestionsAsync(
        IChatCompletionService chat,
        string answer,
        OpenAIPromptExecutionSettings settings,
        CancellationToken cancellationToken)
    {
        var followUpQuestionChat = new ChatHistory(@"You are a helpful AI assistant");
        followUpQuestionChat.AddUserMessage($@"Generate three follow-up question based on the answer you just generated.
# Answer
{answer}

# Format of the response
Return the follow-up question as a json string list. Don't put your answer between ```json and ```, return the json string directly.
e.g.
[
    ""What is the deductible?"",
    ""What is the co-pay?"",
    ""What is the out-of-pocket maximum?""
]");

        var followUpQuestions = await chat.GetChatMessageContentAsync(
            followUpQuestionChat,
            settings,
            cancellationToken: cancellationToken);

        var followUpQuestionsJson = followUpQuestions.Content ??
            throw new InvalidOperationException("Failed to get follow-up questions");
        var followUpQuestionsObject = JsonSerializer.Deserialize<JsonElement>(followUpQuestionsJson);
        return followUpQuestionsObject.EnumerateArray()
            .Select(x => x.GetString()!)
            .ToArray();
    }
}
