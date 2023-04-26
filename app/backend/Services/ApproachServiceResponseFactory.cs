// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Services;

internal sealed class ApproachServiceResponseFactory
{
    private readonly ILogger<ApproachServiceResponseFactory> _logger;
    private readonly IEnumerable<IApproachBasedService> _approachBasedServices;
    private readonly IMemoryCache _cache;

    public ApproachServiceResponseFactory(
        ILogger<ApproachServiceResponseFactory> logger,
        IEnumerable<IApproachBasedService> services, IMemoryCache cache) =>
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

        var approachResponse =
            await _cache.GetOrCreateAsync(
                new CacheKey(approach, question, overrides),
                async entry =>
                {
                    // Cache each unique request for 30 mins and log when evicted...
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
                    entry.RegisterPostEvictionCallback(OnPostEviction, _logger);

                    var response = await service.ReplyAsync(question, overrides, cancellationToken);

                    _logger.LogInformation("{Approach}\n{Response}", approach, response);

                    return response;

                    static void OnPostEviction(
                        object key, object? value, EvictionReason reason, object? state)
                    {
                        if (value is ApproachResponse response &&
                            state is ILogger<ApproachServiceResponseFactory> logger)
                        {
                            logger.LogInformation(
                                "Evicted cached approach response: {Response}",
                                response);
                        }
                    }
                });

        return approachResponse ?? throw new AIException(
            AIException.ErrorCodes.ServiceError,
            $"The approach response for '{approach}' was null.");
    }
}

readonly file record struct CacheKey(
    Approach Approach,
    string Question,
    RequestOverrides? Overrides);
