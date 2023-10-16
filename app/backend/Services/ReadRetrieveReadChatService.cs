// Copyright (c) Microsoft. All rights reserved.

using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Azure;
using Microsoft.SemanticKernel.AI.ChatCompletion;

namespace MinimalApi.Services;

public class ReadRetrieveReadChatService
{
    private readonly SearchClient _searchClient;
    private readonly IKernel _kernel;
    private readonly IConfiguration _configuration;

    public ReadRetrieveReadChatService(
        SearchClient searchClient,
        OpenAIClient client,
        IConfiguration configuration)
    {
        _searchClient = searchClient;
        var deployedModelName = configuration["AzureOpenAiChatGptDeployment"] ?? throw new ArgumentNullException();
        var kernelBuilder = Kernel.Builder.WithAzureChatCompletionService(deployedModelName, client);
        var embeddingModelName = configuration["AzureOpenAiEmbeddingDeployment"];
        if (!string.IsNullOrEmpty(embeddingModelName))
        {
            var endpoint = configuration["AzureOpenAiServiceEndpoint"] ?? throw new ArgumentNullException();
            kernelBuilder = kernelBuilder.WithAzureTextEmbeddingGenerationService(embeddingModelName, endpoint, new DefaultAzureCredential());
        }
        _kernel = kernelBuilder.Build();
        _configuration = configuration;
    }

    public async Task<ApproachResponse> ReplyAsync(
        ChatTurn[] history,
        RequestOverrides? overrides,
        CancellationToken cancellationToken = default)
    {
        var top = overrides?.Top ?? 3;
        var useSemanticCaptions = overrides?.SemanticCaptions ?? false;
        var useSemanticRanker = overrides?.SemanticRanker ?? false;
        var excludeCategory = overrides?.ExcludeCategory ?? null;
        var filter = excludeCategory is null ? null : $"category ne '{excludeCategory}'";
        IChatCompletion chat = _kernel.GetService<IChatCompletion>();
        ITextEmbeddingGeneration? embedding = _kernel.GetService<ITextEmbeddingGeneration>();
        float[]? embeddings = null;
        var question = history.LastOrDefault()?.User is { } userQuestion
            ? userQuestion
            : throw new InvalidOperationException("Use question is null");
        if (overrides?.RetrievalMode != "Text" && embedding is not null)
        {
            embeddings = (await embedding.GenerateEmbeddingAsync(question)).ToArray();
        }

        // step 1
        // use llm to get query if retrieval mode is not vector
        string? query = null;
        if (overrides?.RetrievalMode != "Vector")
        {
            var getQueryChat = chat.CreateNewChat(@"You are a helpful AI assistant, generate search query for followup question.
Make your respond simple and precise. Return the query only, do not return any other text.
e.g.
Northwind Health Plus AND standard plan.
standard plan AND dental AND employee benefit.
");

            getQueryChat.AddUserMessage(question);
            var result = await chat.GetChatCompletionsAsync(
                getQueryChat,
                new ChatRequestSettings
                {
                    Temperature = 0,
                    MaxTokens = 128,
                },
                cancellationToken);

            if (result.Count != 1)
            {
                throw new InvalidOperationException("Failed to get search query");
            }

            query = result[0].ModelResult.GetOpenAIChatResult().Choice.Message.Content;
        }
        
        // step 2
        // use query to search related docs
        var documentContents = await _searchClient.QueryDocumentsAsync(query, embeddings, overrides, cancellationToken);

        if (string.IsNullOrEmpty(documentContents))
        {
            documentContents = "no source available.";
        }

        Console.WriteLine(documentContents);
        // step 3
        // put together related docs and conversation history to generate answer
        var answerChat = chat.CreateNewChat($@"You are a system assistant who helps the company employees with their healthcare plan questions, and questions about the employee handbook. Be brief in your answers");

        // add chat history
        foreach (var turn in history)
        {
            answerChat.AddUserMessage(turn.User);
            if (turn.Bot is { } botMessage)
            {
                answerChat.AddAssistantMessage(botMessage);
            }
        }

        // format prompt
        answerChat.AddUserMessage(@$" ## Source ##
{documentContents}
## End ##

You answer needs to be a json object with the following format.
{{
    ""answer"": // the answer to the question, add a source reference to the end of each sentence. e.g. Apple is a fruit [reference1.pdf]. If no source available, put the answer as I don't know.
    ""thoughts"": // brief thoughts on how you came up with the answer, e.g. what sources you used, what you thought about, etc.
}}");

        // get answer
        var answer = await chat.GetChatCompletionsAsync(
                       answerChat,
                       new ChatRequestSettings
                       {
                           Temperature = overrides?.Temperature ?? 0.7,
                           MaxTokens = 1024,
                       },
                       cancellationToken);
        var answerJson = answer[0].ModelResult.GetOpenAIChatResult().Choice.Message.Content;
        var answerObject = JsonSerializer.Deserialize<JsonElement>(answerJson);
        var ans = answerObject.GetProperty("answer").GetString() ?? throw new InvalidOperationException("Failed to get answer");
        var thoughts = answerObject.GetProperty("thoughts").GetString() ?? throw new InvalidOperationException("Failed to get thoughts");

        // step 4
        // add follow up questions if requested
        if (overrides?.SuggestFollowupQuestions is true)
        {
            var followUpQuestionChat = chat.CreateNewChat(@"You are a helpful AI assistant");
            followUpQuestionChat.AddUserMessage($@"Generate three follow-up question based on the answer you just generated.
# Answer
{ans}

# Format of the response
Return the follow-up question as a json string list.
e.g.
[
    ""What is the deductible?"",
    ""What is the co-pay?"",
    ""What is the out-of-pocket maximum?""
]");

            var followUpQuestions = await chat.GetChatCompletionsAsync(
                               followUpQuestionChat,
                               new ChatRequestSettings
                               {
                                   Temperature = 0,
                                   MaxTokens = 256,
                               },
                               cancellationToken);

            var followUpQuestionsJson = followUpQuestions[0].ModelResult.GetOpenAIChatResult().Choice.Message.Content;
            var followUpQuestionsObject = JsonSerializer.Deserialize<JsonElement>(followUpQuestionsJson);
            var followUpQuestionsList = followUpQuestionsObject.EnumerateArray().Select(x => x.GetString()).ToList();
            foreach (var followUpQuestion in followUpQuestionsList)
            {
                ans += $" <<{followUpQuestion}>> ";
            }
        }
        return new ApproachResponse(
            DataPoints: documentContents.Split('\r'),
            Answer: ans,
            Thoughts: thoughts,
            CitationBaseUrl: _configuration.ToCitationBaseUrl());
    }
}
