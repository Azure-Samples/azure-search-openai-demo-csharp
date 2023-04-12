// Copyright (c) Microsoft. All rights reserved.

namespace Shared.Models;

public record RequestOverrides
{
    public bool? SemanticRanker { get; set; }
    public bool? SemanticCaptions { get; set; }
    public string? ExcludeCategory { get; set; }
    public int? Top { get; set; } = 1;
    public int? Temperature { get; set; }
    public string? PromptTemplate { get; set; }
    public string? PromptTemplatePrefix { get; set; }
    public string? PromptTemplateSuffix { get; set; }
    public bool? SuggestFollowupQuestions { get; set; }
}
