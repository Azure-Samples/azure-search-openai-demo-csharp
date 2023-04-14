// Copyright (c) Microsoft. All rights reserved.

using Backend.Services.Skills;
using Microsoft.SemanticKernel.CoreSkills;

namespace Backend.Services;

public class ReadDecomposeAskApproachService
{
    private readonly SearchClient _searchClient;
    private readonly ILogger? _logger;
    private readonly AzureOpenAITextCompletionService _completionService;
    private const string AnswerPromptPrefix = """
        Answer questions using the given source only. For tabular information return it as an HTML table. Do not return markdown format.
        Each source has a name followed by a colon and the actual information, always include the source name for each fact you use in the answer.
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
        {{$input}}

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

        ### EXAMPLE reply
        true
        ###

        Your reply:
        """;

    private const string ExplainPrefix = """
        Explain why the knowledge is not enough to answer the question.

        Knowledge:
        {{$knowledge}}

        Question:
        {{$question}}

        Your reply:
        """;

    private const string GenerateLookupPrompt = """
        Generate lookup term from explanation.

        ### EXAMPLE:
        Explanation: 'The employee handbook does not mention the deductible for the employee plan for a visit to Overlake in Bellevue.'
        Lookup term: 'deductible for the employee plan for a visit to Overlake in Bellevue'

        Explanation: 'The product manager is not mentioned in given knowledge'
        Lookup term: 'product manager'

        Explanation: 'The eye exam is not mentioned in given knowledge'
        Lookup term: 'eye exam'

        Explanation: 'What is included in my Northwind Health Plus plan that is not in standard?'
        Lookup term: 'Northwind Health Plus plan'
        ###

        Explanation:
        {{$explanation}}
        Lookup term:
        """;

    private const string SummarizeThoughtProcessPrompt = """
        Summarize the entire process you take from question to answer. Describe in detail that
        - How you generate lookup term based on question
        - What useful information you find that help you answer the question
        - how you form the answer based on the information you find
        You can use markdown format.

        question:
        {{$question}}

        query:
        {{$query}}

        information:
        {{$knowledge}}

        answer:
        {{$answer}}

        your summaize:
        """;

    private const string PlannerPrefix = """
        Check if you can answer the given quesiton with knowledge. If yes return answer, otherwise do the following steps until you get the answer:
         - explain why you can't answer the question
         - generating query from explanation
         - use query to lookup or search information, and append the lookup or search result to $knowledge

        Then, save answer to $ANSWER.
        Then, save summarize to $SUMMARY.
        """;

    public ReadDecomposeAskApproachService(SearchClient searchClient, AzureOpenAITextCompletionService completionService, ILogger? logger = null)
    {
        _searchClient = searchClient;
        _completionService = completionService;
        _logger = logger;
    }

    public async Task<AnswerResponse> ReplyAsync(string question, RequestOverrides? overrides)
    {
        var kernel = Kernel.Builder.Build();
        kernel.Config.AddTextCompletionService("openai", (kernel) => _completionService, true);
        kernel.ImportSkill(new RetrieveRelatedDocumentSkill(_searchClient, overrides));
        kernel.ImportSkill(new LookupSkill(_searchClient, overrides));
        kernel.ImportSkill(new UpdateContextVariableSkill());
        kernel.CreateSemanticFunction(ReadDecomposeAskApproachService.AnswerPromptPrefix, functionName: "Answer", description: "answer question",
            maxTokens: 1024, temperature: overrides?.Temperature ?? 0.7);
        kernel.CreateSemanticFunction(ReadDecomposeAskApproachService.ExplainPrefix, functionName: "Explain", description: "explain if knowledge is enough with reason", temperature: 1,
            presencePenalty: 0.5, frequencyPenalty: 0.5);
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
            if (!result.Variables.ToPlan().IsSuccessful)
            {
                Console.WriteLine(result.Variables.ToPlan().PlanString);
                throw new InvalidOperationException(result.Variables.ToPlan().Result);
            }
            sb.AppendLine($"Step {step++} - Execution results:\n");
            sb.AppendLine(result.Variables.ToPlan().Result + "\n");

            executingResult = result;
        }
        while (!executingResult.Variables.ToPlan().IsComplete);

        //Console.WriteLine(sb.ToString());

        return new AnswerResponse(
               DataPoints: executingResult["knowledge"].ToString().Split('\r'),
               Answer: executingResult.Variables["Answer"],
               Thoughts: executingResult.Variables["SUMMARY"].Replace("\n", "<br>"));
    }
}
