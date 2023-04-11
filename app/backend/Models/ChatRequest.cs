﻿// Copyright (c) Microsoft. All rights reserved.

namespace Backend.Models;

public record ChatTurn(string User, string? Bot);

public record ChatRequest(ChatTurn[] History, string Approach, RequestOverrides? Overrides);
