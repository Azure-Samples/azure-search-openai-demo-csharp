// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Services;

internal sealed class ReadRetrieveReadApproachService : IApproachBasedService
{
    private IKernel? _kernel;
    private readonly SearchClient _searchClient;
    private readonly AzureOpenAITextCompletionService _completionService;

    private const string PlanPrompt = """
        Retrieve infomation of question and append to $knowledge,
        Answer question and set to $Answer.
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

Sources:
info1.txt: deductibles depend on whether you are in-network or out-of-network. In-network deductibles are $500 for employees and $1000 for families. Out-of-network deductibles are $1000 for employees and $2000 for families.
info2.pdf: Overlake is in-network for the employee plan.
info3.pdf: Overlake is the name of the area that includes a park and ride near Bellevue.
info4.pdf: In-network institutions include Overlake, Swedish, and others in the region

Answer:
In-network deductibles are $500 for employees and $1000 for families [info1.txt] and Overlake is in-network for the employee plan [info2.pdf][info4.pdf].

###
Question:
{{$question}}

Sources:
{{$knowledge}}

Answer:
""";

    public Approach Approach => Approach.ReadRetrieveRead;

    public ReadRetrieveReadApproachService(SearchClient searchClient, AzureOpenAITextCompletionService service)
    {
        _searchClient = searchClient;
        _completionService = service;
    }

    public async Task<ApproachResponse> ReplyAsync(string question, RequestOverrides? overrides)
    {
        _kernel = Kernel.Builder.Build();
        _kernel.Config.AddTextCompletionService("openai", (_kernel) => _completionService, true);
        _kernel.ImportSkill(new RetrieveRelatedDocumentSkill(_searchClient, overrides));
        _kernel.ImportSkill(new UpdateContextVariableSkill());
        _kernel.CreateSemanticFunction(ReadRetrieveReadApproachService.Prefix, functionName: "Answer", description: "answer question",
            maxTokens: 1_024, temperature: overrides?.Temperature ?? 0.7);
        var planner = _kernel.ImportSkill(new PlannerSkill(_kernel));
        var sb = new StringBuilder();

        var executingResult = await _kernel.RunAsync(ReadRetrieveReadApproachService.PlanPrompt, planner["CreatePlan"]);
        var step = 1;
        executingResult.Variables["question"] = question;
        Console.WriteLine(executingResult.Variables.ToPlan().PlanString);

        do
        {
            var result = await _kernel.RunAsync(executingResult.Variables, planner["ExecutePlan"]);
            var plan = result.Variables.ToPlan();

            if (!plan.IsSuccessful)
            {
                throw new InvalidOperationException(result.Variables.ToPlan().Result);
            }
            
            sb.AppendLine($"Step {step++} - Execution results:\n");
            sb.AppendLine(plan.PlanString + "\n");
            sb.AppendLine(plan.Result + "\n");
            executingResult = result;
        }
        while (!executingResult.Variables.ToPlan().IsComplete);

        return new ApproachResponse(
               DataPoints: executingResult["knowledge"].ToString().Split('\r'),
               Answer: executingResult["Answer"],
               Thoughts: sb.ToString().Replace("\n", "<br>"));
    }
}
