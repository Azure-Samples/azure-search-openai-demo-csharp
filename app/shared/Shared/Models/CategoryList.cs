// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Shared.Models;

public record CategoryList
{
    [JsonPropertyName("categories")]
    public List<string> Categories { get; set; } = new();
}
