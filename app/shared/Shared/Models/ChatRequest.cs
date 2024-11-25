// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Shared.Models;

public record class ChatRequest(
    [property: JsonPropertyName("messages")] ChatMessage[] History,
    [property: JsonPropertyName("overrides")] RequestOverrides? Overrides
    ) : ApproachRequest(Approach.RetrieveThenRead)
{
    public string? LastUserQuestion => History?.Last(m => m.Role == "user")?.Content;
}
