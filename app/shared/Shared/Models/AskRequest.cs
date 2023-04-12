// Copyright (c) Microsoft. All rights reserved.

namespace Shared.Models;

public record AskRequest(
    string Question,
    Approach Approach,
    RequestOverrides? Overrides = null);
