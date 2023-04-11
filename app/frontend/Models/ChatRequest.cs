// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Models;

public record ChatRequest(
    ChatTurn[] History,
    Approach Approach,
    AskRequestOverrides? Overrides);
