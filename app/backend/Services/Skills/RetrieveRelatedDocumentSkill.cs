// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Services.Skills;

public sealed class RetrieveRelatedDocumentSkill
{
    private readonly SearchClient _searchClient;
    private readonly RequestOverrides? _requestOverrides;
    private readonly string? _filter;

    public RetrieveRelatedDocumentSkill(SearchClient searchClient, RequestOverrides? requestOverrides)
    {
        _searchClient = searchClient;
        _requestOverrides = requestOverrides;
        _filter = _requestOverrides?.ExcludeCategory is null ? null : $"category ne '{_requestOverrides.ExcludeCategory}'";
    }

    [SKFunction("Search more information")]
    [SKFunctionName("Search")]
    [SKFunctionInput(Description = "search query")]
    public async Task<string> QueryAsync(string searchQuery, SKContext context)
    {
        if (searchQuery is string query)
        {
            var result = await _searchClient.QueryDocumentsAsync(query,
                _requestOverrides?.Top ?? 3,
                useSemanticCaptions: _requestOverrides?.SemanticCaptions ?? false,
                useSemanticRanker: _requestOverrides?.SemanticRanker ?? false,
                filter: _filter);

            return result;
        }

        throw new AIException(
            AIException.ErrorCodes.ServiceError,
            "Query skill failed to get query from context");
    }
}
