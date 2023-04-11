﻿// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Identity.Client;

namespace Backend.Services;

public class ReadRetreiveReadChatService
{
    private readonly SearchClient _searchClient;
    private readonly IKernel _kernel;
    private const string FollowUpQuestionsPrompt = """
        Generate three very brief follow-up questions that the user would likely ask next about their healthcare plan and employee handbook. 
        Use double angle brackets to reference the questions, e.g. <<Are there exclusions for prescriptions?>>.
        Try not to repeat questions that have already been asked.
        Only generate questions and do not generate any text before or after the questions, such as 'Next Questions'
        """;

    private const string AnswerPromptTemplate = """
            <|im_start|>
            system Assistant helps the company employees with their healthcare plan questions, and questions about the employee handbook. Be brief in your answers.
            Answer ONLY with the facts listed in the list of sources below. If there isn't enough information below, say you don't know. Do not generate answers that don't use the sources below. If asking a clarifying question to the user would help, ask the question.
            For tabular information return it as an html table. Do not return markdown format.
            Each source has a name followed by colon and the actual information, always include the source name for each fact you use in the response. Use square brakets to reference the source, e.g. [info1.txt]. Don't combine sources, list each source separately, e.g. [info1.txt][info2.pdf].
            {{$follow_up_questions_prompt}}
            {{$injected_prompt}}
            Sources:
            {{$sources}}
            <|im_end|>
            {{$chat_history}}
            """;

    public ReadRetreiveReadChatService(SearchClient searchClient, IKernel kernel)
    {
        _searchClient = searchClient;
        _kernel = kernel;
    }

    public async Task<AnswerResponse> ReplyAsync(ChatTurn[] history, RequestOverrides? overrides)
    {
        var top = overrides.Top ?? 3;
        var useSemanticCaptions = overrides.SemanticCaptions ?? false;
        var useSemanticRanker = overrides.SemanticRanker ?? false;
        var excludeCategory = overrides.ExcludeCategory ?? null;
        var filter = excludeCategory is null ? null : $"category ne '{excludeCategory}'";
        // step 1
        // use llm to get query

        var queryFunction = CreateQueryPromptFunction(history, overrides);
        var context = new ContextVariables();
        var historyText = Utils.GetChatHistoryAsText(history, includeLastTurn: false);
        context["chat_history"] = historyText;
        context["question"] = history.Last().User;
        var query = await _kernel.RunAsync(context, queryFunction);
        // step 2
        // use query to search related docs
        var  documentContents = await Utils.QueryDocumentsAsync(query.Result, _searchClient, top, filter, useSemanticRanker, useSemanticCaptions);

        // step 3
        // use llm to get answer
        var answerContext = new ContextVariables();
        ISKFunction answerFunction;
        string prompt;
        answerContext["chat_history"] = Utils.GetChatHistoryAsText(history);
        answerContext["sources"] = documentContents;
        if (overrides.SuggestFollowupQuestions is true)
        {
            answerContext["follow_up_questions_prompt"] = ReadRetreiveReadChatService.FollowUpQuestionsPrompt;
        }
        else
        {
            answerContext["follow_up_questions_prompt"] = string.Empty;         
        }

        if (overrides.PromptTemplate is null)
        {
            answerContext["$injected_prompt"] = string.Empty;
            answerFunction = CreateAnswerPromptFunction(ReadRetreiveReadChatService.AnswerPromptTemplate, overrides);
            prompt = ReadRetreiveReadChatService.AnswerPromptTemplate;
        }
        else if (overrides.PromptTemplate.StartsWith(">>>"))
        {
            answerContext["$injected_prompt"] = overrides.PromptTemplate[3..];
            answerFunction = CreateAnswerPromptFunction(ReadRetreiveReadChatService.AnswerPromptTemplate, overrides);
            prompt = ReadRetreiveReadChatService.AnswerPromptTemplate;

        }
        else if(overrides.PromptTemplate is string promptTemplate)
        {
            answerFunction = CreateAnswerPromptFunction(promptTemplate, overrides);
            prompt = promptTemplate;
        }
        else
        {
            throw new ApplicationException("unknown inject prompt");
        }

        var ans = await _kernel.RunAsync(answerContext, answerFunction);
        prompt = await _kernel.PromptTemplateEngine.RenderAsync(prompt, ans);
        return new AnswerResponse(
            DataPoints: documentContents.Split('\r'),
            Answer: ans.Result,
            Thoughts: $"Searched for:<br>{query}<br><br>Prompt:<br>{prompt.Replace("\n", "<br>")}");
    }

    private ISKFunction CreateQueryPromptFunction(ChatTurn[] history, RequestOverrides overrides)
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

    private ISKFunction CreateAnswerPromptFunction(string answerTemplate, RequestOverrides overrides)
    {
        return _kernel.CreateSemanticFunction(answerTemplate,
                       temperature: overrides.Temperature ?? 0.7,
                       maxTokens: 1024,
                       stopSequences: new[] { "<|im_end|>", "<|im_start|>" });
    }
}
