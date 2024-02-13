// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Services;

public class ReadRetrieveReadChatService
{
    #region Fields

    private readonly ISearchService _searchClient;
    private readonly IConfiguration _configuration;
    private readonly Kernel _kernel;

    #endregion Fields

    #region Contructor/s

    public ReadRetrieveReadChatService(
        ISearchService searchClient,
        OpenAIClient client,
        IConfiguration configuration)
    {
        _searchClient = searchClient;
        _configuration = configuration;

        var deployedModelName = configuration["AzureOpenAiChatGptDeployment"];
        ArgumentNullException.ThrowIfNullOrWhiteSpace(deployedModelName);

        var kernelBuilder = Kernel.CreateBuilder();

        kernelBuilder.AddAzureOpenAIChatCompletion(
            deployedModelName,  // The name of your deployment (e.g., "gpt-35-turbo")
            client
        );

        var embeddingModelName = configuration["AzureOpenAiEmbeddingDeployment"];
        if (!string.IsNullOrEmpty(embeddingModelName))
        {
            var endpoint = configuration["AZURE_OPENAI_ENDPOINT"];
            ArgumentNullException.ThrowIfNullOrWhiteSpace(endpoint);

#pragma warning disable SKEXP0011 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            kernelBuilder.AddAzureOpenAITextEmbeddingGeneration(
                embeddingModelName, endpoint, new DefaultAzureCredential()
            );
#pragma warning restore SKEXP0011 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        }

        _kernel = kernelBuilder.Build();
    }

    #endregion Contructor/s

    #region Public Methods

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
        IChatCompletionService chat = _kernel.GetRequiredService<IChatCompletionService>();
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        ITextEmbeddingGenerationService? embedding = _kernel.GetRequiredService<ITextEmbeddingGenerationService>();
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        float[]? embeddings = null;
        var question = history.LastOrDefault()?.User is { } userQuestion
            ? userQuestion
            : throw new InvalidOperationException("Use question is null");

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        if (overrides?.RetrievalMode != RetrievalMode.Text && embedding is not null)
        {
            embeddings = (await embedding.GenerateEmbeddingAsync(question, cancellationToken: cancellationToken)).ToArray();
        }
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        // step 1
        // use llm to get query if retrieval mode is not vector
        string? query = null;
        if (overrides?.RetrievalMode != RetrievalMode.Vector)
        {
            var chatHistory = new ChatHistory(@"You are a helpful AI assistant, generate search query for followup question.
                Make your respond simple and precise. Return the query only, do not return any other text.
                e.g.
                Northwind Health Plus AND standard plan.
                standard plan AND dental AND employee benefit.");
            chatHistory.AddUserMessage(question);

            var result = await chat.GetChatMessageContentAsync(
                chatHistory,
                cancellationToken: cancellationToken);

            if (result is null)
            {
                throw new InvalidOperationException("Failed to get search query");
            }

            query = result.Content;
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

        Console.WriteLine(documentContents);
        // step 3
        // put together related docs and conversation history to generate answer
        var answerChatHistory = new ChatHistory(
            "You are a system assistant who helps the company employees with their healthcare " +
            "plan questions, and questions about the employee handbook. Be brief in your answers");

        // add chat history
        foreach (var turn in history)
        {
            answerChatHistory.AddUserMessage(turn.User);
            if (turn.Bot is { } botMessage)
            {
                answerChatHistory.AddAssistantMessage(botMessage);
            }
        }

        // format prompt
        answerChatHistory.AddUserMessage(@$" ## Source ##
            {documentContents}
            ## End ##

            You answer needs to be a json object with the following format.
            {{
                ""answer"": // the answer to the question, add a source reference to the end of each sentence. e.g. Apple is a fruit [reference1.pdf][reference2.pdf]. If no source available, put the answer as I don't know.
                ""thoughts"": // brief thoughts on how you came up with the answer, e.g. what sources you used, what you thought about, etc.
            }}");

        // get answer
        var answer = await chat.GetChatMessageContentAsync(
                       answerChatHistory,
                       cancellationToken: cancellationToken);

        var answerJson = answer.Content ?? "{}";
        var answerObject = JsonSerializer.Deserialize<JsonElement>(answerJson);
        var ans = answerObject.GetProperty("answer").GetString() ?? throw new InvalidOperationException("Failed to get answer");
        var thoughts = answerObject.GetProperty("thoughts").GetString() ?? throw new InvalidOperationException("Failed to get thoughts");

        // step 4
        // add follow up questions if requested
        if (overrides?.SuggestFollowupQuestions is true)
        {
            var followUpQuestionChatHistory = new ChatHistory(@"You are a helpful AI assistant");
            followUpQuestionChatHistory.AddUserMessage($@"Generate three follow-up question based on the answer you just generated.
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

            var followUpQuestions = await chat.GetChatMessageContentAsync(
                followUpQuestionChatHistory,
                cancellationToken: cancellationToken);

            var followUpQuestionsJson = followUpQuestions.Content ?? "{}";
            var followUpQuestionsObject = JsonSerializer.Deserialize<JsonElement>(followUpQuestionsJson);
            var followUpQuestionsList = followUpQuestionsObject.EnumerateArray().Select(x => x.GetString()).ToList();
            foreach (var followUpQuestion in followUpQuestionsList)
            {
                ans += $" <<{followUpQuestion}>> ";
            }
        }

        return new ApproachResponse(
            DataPoints: documentContentList,
            Images: null,
            Answer: ans,
            Thoughts: thoughts,
            CitationBaseUrl: _configuration.ToCitationBaseUrl());
    }

    #endregion Public Methods
}
