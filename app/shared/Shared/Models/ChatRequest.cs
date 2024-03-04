// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Shared.Models;

public record class ChatRequest : ApproachRequest
{
    public ChatRequest(ChatMessage[] history, RequestOverrides? overrides = null)
        : base(Approach.RetrieveThenRead)
    {
        History = history;
        Overrides = overrides;
    }

    [JsonPropertyName("messages")]
    public ChatMessage[] History { get; }

    [JsonPropertyName("overrides")]
    public RequestOverrides? Overrides { get; }


    public string? LastUserQuestion => History?.Last(m => m.Role == "user")?.Content;
}
