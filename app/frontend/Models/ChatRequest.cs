// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Models;

public record ChatRequest(
    ChatTurn[] History,
    Approaches Approach,
    AskRequestOverrides? Overrides);
