﻿// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Services;

public sealed class ApproachServiceResponseFactory
{
    private readonly IEnumerable<IApproachBasedService> _approachBasedServices;

    public ApproachServiceResponseFactory(IEnumerable<IApproachBasedService> services) =>
        _approachBasedServices = services;

    public async Task<ApproachResponse> GetApproachResponseAsync(
        Approach approach, string question, RequestOverrides? overrides = null)
    {
        var service = _approachBasedServices.SingleOrDefault(s => s.Approach == approach)
            ?? throw new ArgumentOutOfRangeException(
                nameof(approach), $"Approach: {approach} value isn't supported.");

        var approachResponse = await service.ReplyAsync(question, overrides);

        return approachResponse ?? throw new AIException(
            AIException.ErrorCodes.ServiceError,
            $"The approach response for '{approach}' was null.");
    }
}
