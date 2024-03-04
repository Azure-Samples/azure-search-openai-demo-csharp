// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Shared.Models;

public record ChatMessage
{
    public ChatMessage(string role, string content)
    {
        Role = role;
        Content = content;
    }
    [JsonPropertyName("role")]
    public string Role { get; init; }

    [JsonPropertyName("content")]
    public string Content { get; init; }

    public bool IsUser => Role == "user";
}
