// Copyright (c) Microsoft. All rights reserved.

namespace Shared.Models;

public record ChatRequest(
    ChatTurn[] History,
    Approach Approach,
    RequestOverrides? Overrides = null);
