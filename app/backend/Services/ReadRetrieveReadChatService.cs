// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Services;

public class ReadRetrieveReadChatService
{
    private readonly SearchClient _searchClient;
    private readonly IKernel _kernel;
    private readonly IConfiguration _configuration;

    private const string FollowUpQuestionsPrompt = """
        Generate three very brief follow-up questions that the user would likely ask next about their healthcare plan and employee handbook.
        Use double angle brackets to reference the questions, e.g. <<Are there exclusions for prescriptions?>>.
        Try not to repeat questions that have already been asked.
        Only generate questions and do not generate any text before or after the questions, such as 'Next Questions'
        """;

    private const string AnswerPromptTemplate = """
        <|im_start|>
        You are a system assistant who helps the company employees with their healthcare plan questions, and questions about the employee handbook. Be brief in your answers.
        Answer ONLY with the facts listed in the list of sources below. If there isn't enough information below, say you don't know. Do not generate answers that don't use the sources below. If asking a clarifying question to the user would help, ask the question.
        For tabular information return it as an html table. Do not return markdown format.
        Each source has a name followed by colon and the actual information, always include the full path of source file for each fact you use in the response. Use square brakets to reference the source. Don't combine sources, list each source separately.
        ### Examples
        Sources:
        info1.txt: deductibles depend on whether you are in-network or out-of-network. In-network deductibles are $500 for employees and $1000 for families. Out-of-network deductibles are $1000 for employees and $2000 for families.
        info2.pdf: Overlake is in-network for the employee plan.
        reply: In-network deductibles are $500 for employees and $1000 for families [info1.txt][info2.pdf] and Overlake is in-network for the employee plan [info2.pdf].
        ###
        {{$follow_up_questions_prompt}}
        {{$injected_prompt}}
        Sources:
        {{$sources}}
        <|im_end|>
        {{$chat_history}}
        """;

    public ReadRetrieveReadChatService(
        SearchClient searchClient,
        IKernel kernel,
        IConfiguration configuration)
    {
        _searchClient = searchClient;
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
        var historyText = history.GetChatHistoryAsText(includeLastTurn: false);
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
            Below is a history of the conversation so far, and a new question asked by the user that needs to be answered by searching in a knowledge base about employee healthcare plans and the employee handbook.
            Generate a search query based on the conversation and the new question.
            Do not include cited source filenames and document names e.g info.txt or doc.pdf in the search query terms.
            Do not include any text inside [] or <<>> in the search query terms.
            If the question is not in English, translate the question to English before generating the search query.

            Chat History:
            {{$chat_history}}

            Question:
            {{$question}}

            Search query:
            """;

        return _kernel.CreateSemanticFunction(queryPromptTemplate,
            temperature: 0,
            maxTokens: 32,
            stopSequences: new[] { "\n" });
    }

    private ISKFunction CreateAnswerPromptFunction(string answerTemplate, RequestOverrides? overrides) =>
        _kernel.CreateSemanticFunction(answerTemplate,
            temperature: overrides?.Temperature ?? 0.7,
            maxTokens: 1024,
            stopSequences: new[] { "<|im_end|>", "<|im_start|>" });
}
