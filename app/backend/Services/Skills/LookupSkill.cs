// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel.SkillDefinition;

namespace Backend.Services.Skills;

public class LookupSkill
{
    private readonly SearchClient _searchClient;
    private readonly RequestOverrides? _requestOverrides;

    public LookupSkill(SearchClient searchClient, RequestOverrides? requestOverrides)
    {
        _searchClient = searchClient;
        _requestOverrides = requestOverrides;
    }


    [SKFunction("Look up knowledge")]
    [SKFunctionName("Lookup")]
    [SKFunctionInput(Description = "lookup query")]
    public async Task<string> ExecAsync(string lookupQuery, SKContext context)
    {
        if (lookupQuery is string query)
        {
            var response = await _searchClient.SearchAsync<SearchDocument>(lookupQuery, new SearchOptions
            {
                Size = 1,
                QueryType = SearchQueryType.Full,
                IncludeTotalCount = true,
            });

            var doc = response.Value.GetResults().FirstOrDefault()?.Document;
            if (doc is not null &&
                doc.TryGetValue("content", out var content ) &&
                content is string str && doc.TryGetValue("sourcepage", out var sourcePage) &&
                sourcePage is string sourcePageString)
            {
                str = str.Replace('\r', ' ').Replace('\n', ' ');
                return $"{sourcePage}:{str}";
            }

            return string.Empty;
        }
        throw new AIException(AIException.ErrorCodes.ServiceError, "Query skill failed to get query from context");
    }
}
