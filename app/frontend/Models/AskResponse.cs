// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Models;

public record AskResponse(
    string Answer,
    string? Thoughts,
    string[] DataPoints,
    string? Error);
