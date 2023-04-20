// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Services;

public interface IApproachBasedService
{
    Approach Approach { get; }

    Task<ApproachResponse> ReplyAsync(
        string question,
        RequestOverrides? overrides = null,
        CancellationToken cancellationToken = default);
}
