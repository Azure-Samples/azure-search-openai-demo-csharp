// Copyright (c) Microsoft. All rights reserved.

namespace Backend.Models;

public record class AnswerResponse(
    [property: JsonPropertyName("data_points")] string[] DataPoints,
    [property: JsonPropertyName("answer")] string Answer,
    [property: JsonPropertyName("thoughts")] string Thoughts);
