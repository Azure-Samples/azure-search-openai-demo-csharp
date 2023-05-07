// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel.Planning.Planners;

namespace MinimalApi.Services;

internal sealed class ReadDecomposeAskApproachService : IApproachBasedService
{
    private readonly SearchClient _searchClient;
    private readonly ILogger<ReadDecomposeAskApproachService> _logger;
    private readonly IConfiguration _configuration;
    private readonly AzureOpenAITextCompletionService _completionService;

    private const string AnswerPromptPrefix = """
        Answer questions using the given knowledge ONLY. For tabular information return it as an HTML table. Do not return markdown format.
        Each knowledge has a source name followed by a colon and the actual information, always include the source name for each knowledge you use in the answer.
        Don't cite knowledge that is not available in the knowledge list.
        If you cannot answer using the knowledge list only, say you don't know.

        ### EXAMPLE
        Question: 'What is the deductible for the employee plan for a visit to Overlake in Bellevue?'

        Knowledge:
        info1.txt: deductibles depend on whether you are in-network or out-of-network. In-network deductibles are $500 for employees and $1000 for families. Out-of-network deductibles are $1000 for employees and $2000 for families.
        info2.pdf: Overlake is in-network for the employee plan.
        info3.pdf: Overlake is the name of the area that includes a park and ride near Bellevue.
        info4.pdf: In-network institutions include Overlake, Swedish, and others in the region

        Answer:
        In-network deductibles are $500 for employees and $1000 for families [info1.txt] and Overlake is in-network for the employee plan [info2.pdf][info4.pdf].

        Question: 'What happens in a performance review'

        Knowledge:

        Answer:
        I don't know
        ###
        Knowledge:
        {{$knowledge}}

        Question:
        {{$question}}

        Answer:
        """;

    private const string CheckAnswerAvailablePrefix = """
        Use only 0 or 1 to in your reply. return 0 if the answer is unknown, otherwise return 1.

        Answer:
        {{$answer}}

        ### EXAMPLE
        Answer: I don't know
        Your reply:
        0

        Answer: I don't know the answer
        Your reply:
        0

        Answer: In-network deductibles are $500 for employees and $1000 for families [info1.txt] and Overlake is in-network for the employee plan [info2.pdf][info4.pdf].
        Your reply:
        1
        ###

        Your reply:
        """;

    private const string ExplainPrefix = """
        Summarize the knowledge you need to know to answer the question. Please don't include
        the existing knowledge in the answer

        ### EXAMPLES:
        Knowledge: ''
        Question: 'What is the deductible for the employee plan for a visit to Overlake in Bellevue?'
        Explain: I need to know the information of employee plan and Overlake in Bellevue.

        Knowledge: ''
        Question: 'What happens in a performance review?'
        Your reply: I need to know what's performance review.

        Knowledge: 'Microsoft is a software company'
        Question: 'When is annual review time for employees in Microsoft'
        Explain: I need to know the information of annual review time for employees in Microsoft.

        Knowledge: 'Microsoft is a software company'
        Question: 'What is included in my Northwind Health Plus plan that is not in standard?'
        Explain: I need to know what's Northwind Health Plus Plan and what's not standard in that plan.
        ###
        Knowledge:
        {{$knowledge}}

        Question:
        {{$question}}

        Explain:
        """;

    private const string GenerateKeywordsPrompt = """
        Generate keywords from explanation, separate multiple keywords with comma.

        ### EXAMPLE:
        Explanation: I need to know the information of employee plan and Overlake in Bellevue
        Keywords: employee plan, Overlake Bellevue

        Explanation: I need to know the duty of product manager
        Keywords: product manager

        Explanation: I need to know information of annual eye exam.
        Keywords: annual eye exam

        Explanation: I need to know what's Northwind Health Plus Plan and what's not standard in that plan.
        Keywords: Northwind Health Plus plan
        ###

        Explanation:
        {{$explanation}}
        Keywords:
        """;

    private const string ThoughtProcessPrompt = """
        Describe the thought process of answering the question using given question, explanation, keywords, information, and answer.

        ### EXAMPLE:
        Question: 'how many employees does Microsoft has now'

        Explanation: I need to know the information of Microsoft and its employee number.

        Keywords: Microsoft, employee number

        Information: [google.pdf]: Microsoft has over 144,000 employees worldwide as of 2019.

        Answer: I don't know how many employees does Microsoft has now, but in 2019, Microsoft has over 144,000 employees worldwide.

        Summary:
        The question is about how many employees does Microsoft has now.
        To answer the question, I need to know the information of Microsoft and its employee number.
        I use keywords Microsoft, employee number to search information, and I find the following information:
         - [google.pdf] Microsoft has over 144,000 employees worldwide as of 2019.
        Using that information, I formalize the answer as
         - I don't know how many employees does Microsoft has now, but in 2019, Microsoft has over 144,000 employees worldwide.
        ###

        question:
        {{$question}}

        explanation:
        {{$explanation}}

        keywords:
        {{$keywords}}

        information:
        {{$knowledge}}

        answer:
        {{$answer}}

        summary:
        """;

    private const string PlannerPrefix = """
        do the following steps:
         - explain what you need to know to answer the $question.
         - generating $keywords from explanation.
         - use $keywords to lookup or search information.
         - update information to $knowledge.
         - summarize the entire process and update $summary.
         - answer the question based on the knowledge you have.
        """;

    public Approach Approach => Approach.ReadDecomposeAsk;

    public ReadDecomposeAskApproachService(
        SearchClient searchClient,
        AzureOpenAITextCompletionService completionService,
        ILogger<ReadDecomposeAskApproachService> logger,
        IConfiguration configuration)
    {
        _searchClient = searchClient;
        _completionService = completionService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<ApproachResponse> ReplyAsync(
        string question,
        RequestOverrides? overrides,
        CancellationToken cancellationToken = default)
    {
        var kernel = Kernel.Builder.Build();
        kernel.Config.AddTextCompletionService("openai", (kernel) => _completionService);
        kernel.ImportSkill(new RetrieveRelatedDocumentSkill(_searchClient, overrides));
        kernel.ImportSkill(new LookupSkill(_searchClient, overrides));
        kernel.CreateSemanticFunction(ReadDecomposeAskApproachService.AnswerPromptPrefix, functionName: "Answer", description: "answer question with given knowledge",
            maxTokens: 1024, temperature: overrides?.Temperature ?? 0.7);
        kernel.CreateSemanticFunction(ReadDecomposeAskApproachService.ExplainPrefix, functionName: "Explain", description: "explain", temperature: 0.5,
            presencePenalty: 0.5, frequencyPenalty: 0.5);
        kernel.CreateSemanticFunction(ReadDecomposeAskApproachService.GenerateKeywordsPrompt, functionName: "GenerateKeywords", description: "Generate keywords for lookup or search from given explanation", temperature: 0,
            presencePenalty: 0.5, frequencyPenalty: 0.5);
        kernel.CreateSemanticFunction(ReadDecomposeAskApproachService.ThoughtProcessPrompt, functionName: "Summarize", description: "Summarize the entire process of getting answer.", temperature: overrides?.Temperature ?? 0.7,
            presencePenalty: 0.5, frequencyPenalty: 0.5, maxTokens: 2048);

        var planner = new SequentialPlanner(kernel, new PlannerConfig
        {
            RelevancyThreshold = 0.7,
        });
        var planInstruction = $"{ReadDecomposeAskApproachService.PlannerPrefix}";
        var plan = await planner.CreatePlanAsync(planInstruction);
        plan.State["question"] = question;
        _logger.LogInformation("{Plan}", PlanToString(plan));

        do
        {
            plan = await kernel.StepAsync(question, plan, cancellationToken: cancellationToken);
        } while (plan.HasNextStep);

        return new ApproachResponse(
            DataPoints: plan.State["knowledge"].ToString().Split('\r'),
            Answer: plan.State["Answer"],
            Thoughts: plan.State["SUMMARY"].Replace("\n", "<br>"),
            CitationBaseUrl: _configuration.ToCitationBaseUrl());
    }

    private static string PlanToString(Plan originalPlan)
    {
        return $"Goal: {originalPlan.Description}\n\nSteps:\n" + string.Join("\n", originalPlan.Steps.Select(
            s =>
                $"- {s.SkillName}.{s.Name} {string.Join(" ", s.NamedParameters.Select(p => $"{p.Key}='{p.Value}'"))}{" => " + string.Join(" ", s.NamedOutputs.Where(s => s.Key.ToUpper(System.Globalization.CultureInfo.CurrentCulture) != "INPUT").Select(p => $"{p.Key}"))}"
        ));
    }
}
