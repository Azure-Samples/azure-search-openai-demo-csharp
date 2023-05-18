// Copyright (c) Microsoft. All rights reserved.

using System.Threading;

namespace MinimalApi.Services.Skills;

public sealed class RetrieveRelatedDocumentSkill
{
    private readonly SearchClient _searchClient;
    private readonly RequestOverrides? _requestOverrides;
    private readonly OpenAIClient _openAIClient;
    private readonly string _embeddingModel;

    public RetrieveRelatedDocumentSkill(
        SearchClient searchClient,
        OpenAIClient openAIClient,
        string embeddingModel,
        RequestOverrides? requestOverrides)
    {
        _searchClient = searchClient;
        _requestOverrides = requestOverrides;
        _openAIClient = openAIClient;
        _embeddingModel = embeddingModel;
    }

    [SKFunction("Search more information")]
    [SKFunctionName("Search")]
    [SKFunctionInput(Description = "search query")]
    public async Task<string> QueryAsync(string searchQuery, SKContext context)
    {
        if (searchQuery is string query)
        {
            var questionEmbeddingResponse = await _openAIClient!.GetEmbeddingsAsync(_embeddingModel, new EmbeddingsOptions(searchQuery)
            {
                InputType = "query",
            });
            var embedding = questionEmbeddingResponse.Value.Data.First().Embedding.ToArray();
            var result = await _searchClient.QueryDocumentsAsync(query, embedding, overrides: _requestOverrides);

            return result;
        }

        throw new AIException(
            AIException.ErrorCodes.ServiceError,
            "Query skill failed to get query from context");
    }
}
