// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Services;

internal sealed class ApproachServiceResponseFactory
{
    private readonly ILogger<ApproachServiceResponseFactory> _logger;
    private readonly IEnumerable<IApproachBasedService> _approachBasedServices;
    private readonly IDistributedCache _cache;

    public ApproachServiceResponseFactory(
        ILogger<ApproachServiceResponseFactory> logger,
        IEnumerable<IApproachBasedService> services, IDistributedCache cache) =>
        (_logger, _approachBasedServices, _cache) = (logger, services, cache);

    internal async Task<ApproachResponse> GetApproachResponseAsync(
        Approach approach,
        string question,
        RequestOverrides? overrides = null,
        CancellationToken cancellationToken = default)
    {
        var service =
            _approachBasedServices.SingleOrDefault(service => service.Approach == approach)
            ?? throw new ArgumentOutOfRangeException(
                nameof(approach), $"Approach: {approach} value isn't supported.");

        var key = new CacheKey(approach, question, overrides)
            .ToCacheKeyString();

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        // If the value is cached, return it.
        var cachedValue = await _cache.GetStringAsync(key, cancellationToken);
        if (cachedValue is { Length: > 0 } &&
            JsonSerializer.Deserialize<ApproachResponse>(cachedValue, options) is ApproachResponse cachedResponse)
        {
            _logger.LogDebug(
                "Returning cached value for key ({Key}): {Approach}\n{Response}",
                key, approach, cachedResponse);

            return cachedResponse;
        }

        var approachResponse =
            await service.ReplyAsync(question, overrides, cancellationToken)
            ?? throw new AIException(
                AIException.ErrorCodes.ServiceError,
                $"The approach response for '{approach}' was null.");

        var json = JsonSerializer.Serialize(approachResponse, options);
        var entryOptions = new DistributedCacheEntryOptions
        {
            // Cache each unique request for 30 mins and log when evicted...
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        };

        await _cache.SetStringAsync(key, json, entryOptions, cancellationToken);

        _logger.LogDebug(
            "Returning new value for key ({Key}): {Approach}\n{Response}",
            key, approach, approachResponse);

        return approachResponse;
    }
}

internal readonly record struct CacheKey(
    Approach Approach,
    string Question,
    RequestOverrides? Overrides)
{
    /// <summary>
    /// Converts the given <paramref name="cacheKey"/> instance into a <see cref="string"/>
    /// that will uniquely identify the approach, question and optional override pairing.
    /// </summary>
    internal string ToCacheKeyString()
    {
        var (approach, question, overrides) = this;

        string? overridesString = null;
        if (overrides is { } o)
        {
            static string Bit(bool value) => value ? "1" : "0";

            var bits = $"""
                {Bit(o.SemanticCaptions.GetValueOrDefault())}.{Bit(o.SemanticRanker)}.{Bit(o.SuggestFollowupQuestions)}
                """;

            overridesString =
                $":{o.ExcludeCategory}-{o.PromptTemplate}-{o.PromptTemplatePrefix}-{o.PromptTemplateSuffix}-{bits}";
        }

        return $"""
            {approach}:{question}{overridesString}
            """;
    }
}
