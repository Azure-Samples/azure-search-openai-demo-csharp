// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel.Planning.Planners;

namespace MinimalApi.Services;

internal sealed class ReadRetrieveReadApproachService : IApproachBasedService
{
    private readonly SearchClient _searchClient;
    private readonly AzureOpenAITextCompletionService _completionService;
    private readonly ILogger<ReadRetrieveReadApproachService> _logger;
    private readonly IConfiguration _configuration;

    private const string PlanPrompt = """
        Do the following steps:
         - Search information for $question and save result to $knowledge
         - Answer the $question based on the knowledge you have and save result to $answer.
        """;

    private const string Prefix = """
        You are an intelligent assistant helping Contoso Inc employees with their healthcare plan questions and employee handbook questions.
        Use 'you' to refer to the individual asking the questions even if they ask with 'I'.
        Answer the following question using only the data provided in the sources below.
        For tabular information return it as an HTML table. Do not return markdown format.
        Each source has a name followed by a colon and the actual information, always include the source name for each fact you use in the response.
        If you cannot answer using the sources below, say you don't know.

        ###
        Question: 'What is the deductible for the employee plan for a visit to Overlake in Bellevue?'

        Knowledge:
        info1.txt: deductibles depend on whether you are in-network or out-of-network. In-network deductibles are $500 for employees and $1000 for families. Out-of-network deductibles are $1000 for employees and $2000 for families.
        info2.pdf: Overlake is in-network for the employee plan.
        info3.pdf: Overlake is the name of the area that includes a park and ride near Bellevue.
        info4.pdf: In-network institutions include Overlake, Swedish, and others in the region

        Answer:
        In-network deductibles are $500 for employees and $1000 for families [info1.txt] and Overlake is in-network for the employee plan [info2.pdf][info4.pdf].

        ###
        Question:
        {{$question}}

        Knowledge:
        {{$knowledge}}

        Answer:
        """;

    public Approach Approach => Approach.ReadRetrieveRead;

    public ReadRetrieveReadApproachService(
        SearchClient searchClient,
        AzureOpenAITextCompletionService service,
        ILogger<ReadRetrieveReadApproachService> logger,
        IConfiguration configuration)
    {
        _searchClient = searchClient;
        _completionService = service;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<ApproachResponse> ReplyAsync(
        string question,
        RequestOverrides? overrides,
        CancellationToken cancellationToken = default)
    {
        var kernel = Kernel.Builder.Build();
        kernel.Config.AddTextCompletionService("openai", _ => _completionService);
        kernel.ImportSkill(new RetrieveRelatedDocumentSkill(_searchClient, overrides));
        kernel.CreateSemanticFunction(ReadRetrieveReadApproachService.Prefix, functionName: "Answer", description: "answer question",
            maxTokens: 1_024, temperature: overrides?.Temperature ?? 0.7);
        var planner = new SequentialPlanner(kernel, new PlannerConfig
        {
            RelevancyThreshold = 0.7,
        });
        var sb = new StringBuilder();
        var plan = await planner.CreatePlanAsync(ReadRetrieveReadApproachService.PlanPrompt);
        var step = 1;
        plan.State["question"] = question;
        plan.State["knowledge"] = string.Empty;
        _logger.LogInformation("{Plan}", PlanToString(plan));

        do
        {
            plan = await kernel.StepAsync(plan, cancellationToken: cancellationToken);
            sb.AppendLine($"Step {step++} - Execution results:\n");
            sb.AppendLine(plan.State + "\n");
        } while (plan.HasNextStep);

        return new ApproachResponse(
            DataPoints: plan.State["knowledge"].ToString().Split('\r'),
            Answer: plan.State["Answer"],
            Thoughts: sb.ToString().Replace("\n", "<br>"),
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
