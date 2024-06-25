// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Shared.Models;
public record RequestOverrides
{
    [JsonPropertyName("semantic_ranker")]
    public bool SemanticRanker { get; set; } = false;

    [JsonPropertyName("retrieval_mode")]
    public RetrievalMode RetrievalMode { get; set; } = RetrievalMode.Vector; // available option: Text, Vector, Hybrid

    [JsonPropertyName("semantic_captions")]
    public bool? SemanticCaptions { get; set; }

    [JsonPropertyName("exclude_category")]
    public string? ExcludeCategory { get; set; }

    [JsonPropertyName("top")]
    public int? Top { get; set; } = 3;

    [JsonPropertyName("temperature")]
    public int? Temperature { get; set; }

    [JsonPropertyName("prompt_template")]
    public string? PromptTemplate { get; set; }

    [JsonPropertyName("prompt_template_prefix")]
    public string? PromptTemplatePrefix { get; set; }

    [JsonPropertyName("prompt_template_suffix")]
    public string? PromptTemplateSuffix { get; set; }

    [JsonPropertyName("suggest_followup_questions")]
    public bool? SuggestFollowupQuestions { get; set; } = true;

    [JsonPropertyName("use_gpt4v")]
    public bool? UseGPT4V { get; set; } = false;

    [JsonPropertyName("use_oid_security_filter")]
    public bool? UseOIDSecurityFilter { get; set; } = false;

    [JsonPropertyName("use_groups_security_filter")]
    public bool? UseGroupsSecurityFilter { get; set; } = false;

    [JsonPropertyName("vector_fields")]
    public bool? VectorFields { get; set; } = false;
}
