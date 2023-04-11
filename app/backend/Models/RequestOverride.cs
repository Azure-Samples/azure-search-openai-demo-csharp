// Copyright (c) Microsoft. All rights reserved.

namespace Backend.Models;

//export type AskRequestOverrides = {
//    semanticRanker?: boolean;
//    semanticCaptions?: boolean;
//    excludeCategory?: string;
//    top?: number;
//    temperature?: number;
//    promptTemplate?: string;
//    promptTemplatePrefix?: string;
//    promptTemplateSuffix?: string;
//    suggestFollowupQuestions?: boolean;
//};

public record RequestOverrides(
       bool? SemanticRanker,
       bool? SemanticCaptions,
       string? ExcludeCategory,
       int? Top,
       double? Temperature,
       string? PromptTemplate,
       string? PromptTemplatePrefix,
       string? PromptTemplateSuffix,
       bool? SuggestFollowupQuestions
    );
