// Copyright (c) Microsoft. All rights reserved.

namespace Shared.Models;

public record ChatRequest(
    ChatTurn[] History,
    Approach Approach,
    RequestOverrides? Overrides = null)
{
    public string? LastUserQuestion => History?.LastOrDefault()?.User;
}
