// Copyright (c) Microsoft. All rights reserved.

namespace Backend.Models;

public record ChatRequest(ChatTurn[] History, string Approach, RequestOverrides? Overrides);
