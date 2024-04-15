// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Shared.Models;

public record ChatMessage(
    [property:JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content)
{
    public bool IsUser => Role == "user";
}
