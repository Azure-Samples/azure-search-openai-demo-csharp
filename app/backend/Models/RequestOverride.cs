// Copyright (c) Microsoft. All rights reserved.

namespace Backend.Models;

public record RequestOverrides(
    bool? SemanticRanker,
    bool? SemanticCaptions,
    string? ExcludeCategory,
    int? Top,
    double? Temperature,
    string? PromptTemplate,
    string? PromptTemplatePrefix,
    string? PromptTemplateSuffix,
    bool? SuggestFollowupQuestions);
