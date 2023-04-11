// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Models;

public record AskRequestOverrides
{
    public bool? SemanticRanker { get; init; }
    public bool? SemanticCaptions { get; init; }
    public string? ExcludeCategory { get; init; }
    public int? Top { get; init; }
    public int? Temperature { get; init; }
    public string? PromptTemplate { get; init; }
    public string? PromptTemplatePrefix { get; init; }
    public string? PromptTemplateSuffix { get; init; }
    public bool? SuggestFollowupQuestions { get; init; }
}
