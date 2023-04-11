// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Models;

public record AskRespone(
    string Answer,
    string? Thoughts,
    string[] DataPoints,
    string? Error);
