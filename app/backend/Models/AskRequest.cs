// Copyright (c) Microsoft. All rights reserved.

namespace Backend.Models;

public record AskRequest(string Question, string Approach, RequestOverrides? Overrides);
