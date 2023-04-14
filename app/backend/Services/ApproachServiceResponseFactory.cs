// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Services;

public sealed class ApproachServiceResponseFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ApproachServiceResponseFactory(IServiceProvider serviceProvider) =>
        _serviceProvider = serviceProvider;

    public async Task<ApproachResponse> GetApproachResponseAsync(
        Approach approach, string question, RequestOverrides? overrides = null)
    {
        var approachResponse = approach switch
        {
            Approach.RetrieveThenRead =>
                await _serviceProvider.GetRequiredService<RetrieveThenReadApproachService>()
                    .ReplyAsync(question),

            Approach.ReadRetrieveRead =>
                await _serviceProvider.GetRequiredService<ReadRetrieveReadApproachService>()
                    .ReplyAsync(question, overrides),
            
            Approach.ReadDecomposeAsk =>
                await _serviceProvider.GetRequiredService<ReadDecomposeAskApproachService>()
                    .ReplyAsync(question, overrides),

            _ => throw new ArgumentOutOfRangeException(
                nameof(approach), $"Approach: {approach} value isn't supported.")
        };

        return approachResponse ?? throw new AIException(
            AIException.ErrorCodes.ServiceError,
            $"The approach response for '{approach}' was null.");
    }
}
