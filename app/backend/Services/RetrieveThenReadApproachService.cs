// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Services;

internal sealed class RetrieveThenReadApproachService : IApproachBasedService
{
    private readonly SearchClient _searchClient;

    private const string SemanticFunction = """
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
          Question: {{$question}}?
          
          Sources:
          {{$retrieve}}
          
          Answer:
          """;

    private readonly IKernel _kernel;
    private readonly IConfiguration _configuration;
    private readonly ISKFunction _function;

    public Approach Approach => Approach.RetrieveThenRead;

    public RetrieveThenReadApproachService(SearchClient searchClient, IKernel kernel, IConfiguration configuration)
    {
        _searchClient = searchClient;
        _kernel = kernel;
        _configuration = configuration;
        _function = kernel.CreateSemanticFunction(
            SemanticFunction, maxTokens: 200, temperature: 0.7, topP: 0.5);
    }

    public async Task<ApproachResponse> ReplyAsync(
        string question,
        RequestOverrides? overrides = null,
        CancellationToken cancellationToken = default)
    {
        var text = await _searchClient.QueryDocumentsAsync(question, cancellationToken: cancellationToken);
        var context = _kernel.CreateNewContext();
        context["retrieve"] = text;
        context["question"] = question;

        var answer = await _kernel.RunAsync(context.Variables, cancellationToken, _function);

        return new ApproachResponse(
            DataPoints: text.Split('\r'),
            Answer: answer.ToString(),
            Thoughts: $"Question: {question} \r Prompt: {context.Variables}",
            CitationBaseUrl: _configuration.ToCitationBaseUrl());
    }
}
