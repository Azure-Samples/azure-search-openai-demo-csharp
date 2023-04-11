// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Models;

public record AskRequest(string Question, string Approach, RequestOverrides? Overrides);
