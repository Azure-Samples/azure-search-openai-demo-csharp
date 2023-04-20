// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Services;

internal sealed class ReadDecomposeAskApproachService : IApproachBasedService
{
    private readonly SearchClient _searchClient;
    private readonly ILogger? _logger;
    private readonly AzureOpenAITextCompletionService _completionService;

    private const string AnswerPromptPrefix = """
        Answer questions using the given knowledge only. For tabular information return it as an HTML table. Do not return markdown format.
        Each knowledge has a source name followed by a colon and the actual information, always include the source name for each knowledge you use in the answer.
        If you don't know the answer, say you don't know.

        ### EXAMPLE
        Question: 'What is the deductible for the employee plan for a visit to Overlake in Bellevue?'

        Knowledge:
        info1.txt: deductibles depend on whether you are in-network or out-of-network. In-network deductibles are $500 for employees and $1000 for families. Out-of-network deductibles are $1000 for employees and $2000 for families.
        info2.pdf: Overlake is in-network for the employee plan.
        info3.pdf: Overlake is the name of the area that includes a park and ride near Bellevue.
        info4.pdf: In-network institutions include Overlake, Swedish, and others in the region

        Answer:
        In-network deductibles are $500 for employees and $1000 for families [info1.txt] and Overlake is in-network for the employee plan [info2.pdf][info4.pdf].

        ###
        Knowledge:
        {{$knowledge}}

        Question:
        {{$question}}

        Answer:
        """;

    private const string CheckAnswerAvailablePrefix = """
        return true if you can answer the question with the given knowledge, otherwise return false.

        Knowledge:
        {{$knowledge}}

        Question:
        {{$question}}

        ### EXAMPLE
        Knowledge:

        Question: 'What is the deductible for the employee plan for a visit to Overlake in Bellevue?'

        Your reply: false

        Knowledge: 'Microsoft is a software company'
        Question: 'What is Microsoft'
        Your reply: true
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

    private const string GenerateLookupPrompt = """
        Generate lookup terms from explanation, seperate multiple terms with comma.

        ### EXAMPLE:
        Explanation: I need to know the information of employee plan and Overlake in Bellevue
        Lookup term: employee plan, Overlake Bellevue

        Explanation: I need to know the duty of product manager
        Lookup term: product manager

        Explanation: I need to know information of annual eye exam.
        Lookup term: annual eye exam

        Explanation: I need to know what's Northwind Health Plus Plan and what's not standard in that plan.
        Lookup term: Northwind Health Plus plan
        ###

        Explanation:
        {{$explanation}}
        Lookup term:
        """;

    private const string SummarizeThoughtProcessPrompt = """
        Summarize the entire process you take from question to answer. Describe in detail that
        - What lookup term you generate from question and why you generate that lookup term.
        - What useful information you find that help you answer the question
        - how you form the answer based on the information you find
        - how you formalize the answer
        You can use markdown format.

        question:
        {{$question}}

        lookup term:
        {{$query}}

        information:
        {{$knowledge}}

        answer:
        {{$answer}}

        your summaize:
        """;

    private const string PlannerPrefix = """
        1:Check if you can answer the given question with existing knowledge. If yes return answer, otherwise do the following steps until you get the answer:
         - explain why you can't answer the question
         - generating query from explanation
         - use query to lookup or search information, and append the lookup or search result to $knowledge
        2:Answer to $ANSWER.
        3:Summarize and set to Summary variable.
        """;

    public Approach Approach => Approach.ReadDecomposeAsk;

    public ReadDecomposeAskApproachService(SearchClient searchClient, AzureOpenAITextCompletionService completionService, ILogger? logger = null)
    {
        _searchClient = searchClient;
        _completionService = completionService;
        _logger = logger;
    }

    public async Task<ApproachResponse> ReplyAsync(string question, RequestOverrides? overrides)
    {
        var kernel = Kernel.Builder.Build();
        kernel.Config.AddTextCompletionService("openai", (kernel) => _completionService);
        kernel.ImportSkill(new RetrieveRelatedDocumentSkill(_searchClient, overrides));
        kernel.ImportSkill(new LookupSkill(_searchClient, overrides));
        kernel.ImportSkill(new UpdateContextVariableSkill());
        kernel.CreateSemanticFunction(ReadDecomposeAskApproachService.AnswerPromptPrefix, functionName: "Answer", description: "answer question",
            maxTokens: 1024, temperature: overrides?.Temperature ?? 0.7);
        kernel.CreateSemanticFunction(ReadDecomposeAskApproachService.ExplainPrefix, functionName: "Explain", description: "explain if knowledge is enough with reason", temperature: 1,
            presencePenalty: 0, frequencyPenalty: 0);
        kernel.CreateSemanticFunction(ReadDecomposeAskApproachService.GenerateLookupPrompt, functionName: "GenerateQuery", description: "Generate query for lookup or search from given explanation", temperature: 1,
            presencePenalty: 0.5, frequencyPenalty: 0.5);
        kernel.CreateSemanticFunction(ReadDecomposeAskApproachService.CheckAnswerAvailablePrefix, functionName: "CheckAnswerAvailablity", description: "Check if answer is available, return true if yes, return false if not available", temperature: 1,
            presencePenalty: 0.5, frequencyPenalty: 0.5);
        kernel.CreateSemanticFunction(ReadDecomposeAskApproachService.SummarizeThoughtProcessPrompt, functionName: "Summarize", description: "Summarize the entire process of getting answer.", temperature: 0.3,
            presencePenalty: 0.5, frequencyPenalty: 0.5, maxTokens: 2048);

        var planner = kernel.ImportSkill(new PlannerSkill(kernel));
        var sb = new StringBuilder();

        var planInstruction = $"{ReadDecomposeAskApproachService.PlannerPrefix}";

        var executingResult = await kernel.RunAsync(planInstruction, planner["CreatePlan"]);
        Console.WriteLine(executingResult.Variables.ToPlan().PlanString);
        executingResult.Variables["question"] = question;
        var step = 1;

        do
        {
            var result = await kernel.RunAsync(executingResult.Variables, planner["ExecutePlan"]);
            var plan = result.Variables.ToPlan();

            if (!plan.IsSuccessful)
            {
                Console.WriteLine(plan.PlanString);
                throw new InvalidOperationException(plan.Result);
            }

            sb.AppendLine($"Step {step++} - Execution results:\n");
            sb.AppendLine(plan.Result + "\n");

            executingResult = result;
        } while (!executingResult.Variables.ToPlan().IsComplete);

        return new ApproachResponse(
            DataPoints: executingResult["knowledge"].ToString().Split('\r'),
            Answer: executingResult.Variables["Answer"],
            Thoughts: executingResult.Variables["SUMMARY"].Replace("\n", "<br>"));
    }
}
