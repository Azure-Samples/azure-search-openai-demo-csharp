// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Backend.Models;

public record class AnswerResponse(
    [property: JsonPropertyName("data_points")] string[] DataPoints,
    string Answer,
    string Thoughts);
