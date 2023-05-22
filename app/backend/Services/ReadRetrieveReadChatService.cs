// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Services;

public class ReadRetrieveReadChatService
{
    private readonly SearchClient _searchClient;
    private readonly AzureOpenAIChatCompletionService _completionService;
    private readonly IKernel _kernel;
    private readonly IConfiguration _configuration;

    private const string FollowUpQuestionsPrompt = """
        After answering question, also generate three very brief follow-up questions that the user would likely ask next.
        Use double angle brackets to reference the questions, e.g. <<Are there exclusions for prescriptions?>>.
        Try not to repeat questions that have already been asked.
        Only generate questions and do not generate any text before or after the questions, such as 'Next Questions'
        """;

    private const string AnswerPromptTemplate = """
        <|im_start|>system
        You are a system assistant who helps the company employees with their healthcare plan questions, and questions about the employee handbook. Be brief in your answers.
        Answer ONLY with the facts listed in the list of sources below. If there isn't enough information below, say you don't know. Do not generate answers that don't use the sources below.
        {{$follow_up_questions_prompt}}
        For tabular information return it as an html table. Do not return markdown format.
        Each source has a name followed by colon and the actual information, ALWAYS reference source for each fact you use in the response. Use square brakets to reference the source. List each source separately.
        {{$injected_prompt}}

        Here're a few examples:
        ### Good Example 1 (include source) ###
        Apple is a fruit[reference1.pdf].
        ### Good Example 2 (include multiple source) ###
        Apple is a fruit[reference1.pdf][reference2.pdf].
        ### Good Example 2 (include source and use double angle brackets to reference question) ###
        Microsoft is a software company[reference1.pdf].  <<followup question 1>> <<followup question 2>> <<followup question 3>>
        ### END ###
        Sources:
        {{$sources}}

        Chat history:
        {{$chat_history}}
        <|im_end|>
        <|im_start|>user
        {{$question}}
        <|im_end|>
        <|im_start|>assistant
        """;

    public ReadRetrieveReadChatService(
        SearchClient searchClient,
        AzureOpenAIChatCompletionService completionService,
        IConfiguration configuration)
    {
        _searchClient = searchClient;
        _completionService = completionService;
        var deployedModelName = configuration["AzureOpenAiChatGptDeployment"];
        var kernel = Kernel.Builder.Build();
        kernel.Config.AddTextCompletionService(deployedModelName!, _ => completionService);
        _kernel = kernel;
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

        // step 1
        // use llm to get query
        var queryFunction = CreateQueryPromptFunction(history);
        var context = new ContextVariables();
        var historyText = history.GetChatHistoryAsText(includeLastTurn: true);
        context["chat_history"] = historyText;
        if (history.LastOrDefault()?.User is { } userQuestion)
        {
            context["question"] = userQuestion;
        }
        else
        {
            throw new InvalidOperationException("Use question is null");
        }

        var query = await _kernel.RunAsync(context, cancellationToken, queryFunction);
        // step 2
        // use query to search related docs
        var documentContents = await _searchClient.QueryDocumentsAsync(query.Result, overrides, cancellationToken);

        // step 3
        // use llm to get answer
        var answerContext = new ContextVariables();
        ISKFunction answerFunction;
        string prompt;
        answerContext["chat_history"] = history.GetChatHistoryAsText();
        answerContext["sources"] = documentContents;
        if (overrides?.SuggestFollowupQuestions is true)
        {
            answerContext["follow_up_questions_prompt"] = ReadRetrieveReadChatService.FollowUpQuestionsPrompt;
        }
        else
        {
            answerContext["follow_up_questions_prompt"] = string.Empty;
        }

        if (overrides is null or { PromptTemplate: null })
        {
            answerContext["$injected_prompt"] = string.Empty;
            answerFunction = CreateAnswerPromptFunction(ReadRetrieveReadChatService.AnswerPromptTemplate, overrides);
            prompt = ReadRetrieveReadChatService.AnswerPromptTemplate;
        }
        else if (overrides is not null && overrides.PromptTemplate.StartsWith(">>>"))
        {
            answerContext["$injected_prompt"] = overrides.PromptTemplate[3..];
            answerFunction = CreateAnswerPromptFunction(ReadRetrieveReadChatService.AnswerPromptTemplate, overrides);
            prompt = ReadRetrieveReadChatService.AnswerPromptTemplate;
        }
        else if (overrides?.PromptTemplate is string promptTemplate)
        {
            answerFunction = CreateAnswerPromptFunction(promptTemplate, overrides);
            prompt = promptTemplate;
        }
        else
        {
            throw new InvalidOperationException("Failed to get search result");
        }

        var ans = await _kernel.RunAsync(answerContext, cancellationToken, answerFunction);
        prompt = await _kernel.PromptTemplateEngine.RenderAsync(prompt, ans);
        return new ApproachResponse(
            DataPoints: documentContents.Split('\r'),
            Answer: ans.Result,
            Thoughts: $"Searched for:<br>{query}<br><br>Prompt:<br>{prompt.Replace("\n", "<br>")}",
            CitationBaseUrl: _configuration.ToCitationBaseUrl());
    }

    private ISKFunction CreateQueryPromptFunction(ChatTurn[] history)
    {
        var queryPromptTemplate = """
            <|im_start|>system
            Chat history:
            {{$chat_history}}
            
            Here's a few examples of good search queries:
            ### Good example 1 ###
            Northwind Health Plus AND standard plan
            ### Good example 2 ###
            standard plan AND dental AND employee benefit
            ###

            <|im_end|>
            <|im_start|>system
            Generate search query for followup question. You can refer to chat history for context information. Just return search query and don't include any other information.
            {{$question}}
            <|im_end|>
            <|im_start|>assistant
            """;

        return _kernel.CreateSemanticFunction(queryPromptTemplate,
            temperature: 0,
            maxTokens: 32,
            stopSequences: new[] { "<|im_end|>" });
    }

    private ISKFunction CreateAnswerPromptFunction(string answerTemplate, RequestOverrides? overrides) =>
        _kernel.CreateSemanticFunction(answerTemplate,
            temperature: overrides?.Temperature ?? 0.7,
            maxTokens: 1024,
            stopSequences: new[] { "<|im_end|>" });
}
