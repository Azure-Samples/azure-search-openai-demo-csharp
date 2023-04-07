// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Backend.Models;

public class Reply
{
    [JsonPropertyName("data_points")]
    public string[] DataPoints { get; set; } = { };

    [JsonPropertyName("answer")]
    public string Answer { get; set; } = string.Empty;

    [JsonPropertyName("thoughts")]
    public string Thoughts { get; set; } = string.Empty;
}