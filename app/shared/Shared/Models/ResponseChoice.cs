// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;
using Azure.AI.OpenAI;

namespace Shared.Models;

public record SupportingContentRecord(string Title, string Content);

public record SupportingImageRecord(string Title, string Url);

public record Thoughts
{
    public Thoughts(string Title, string Description, params (string, string)[] props)
    {
        this.Title = Title;
        this.Description = Description;
        this.Props = props;
    }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("props")]
    public (string, string)[] Props { get; set; }
}

public record ResponseContext
{
    public ResponseContext(SupportingContentRecord[]? DataPointsContent, SupportingImageRecord[]? DataPointsImages, string[] FollowupQuestions, Thoughts[] thoughts)
    {
        this.DataPointsContent = DataPointsContent;
        this.DataPointsImages = DataPointsImages;
        this.FollowupQuestions = FollowupQuestions;
        this.Thoughts = thoughts;
    }

    [JsonPropertyName("dataPointsContent")]
    public SupportingContentRecord[]? DataPointsContent { get; set; }

    [JsonPropertyName("dataPointsImages")]
    public SupportingImageRecord[]? DataPointsImages { get; set; }

    [JsonPropertyName("followup_questions")]
    public string[] FollowupQuestions { get; set; }

    [JsonPropertyName("thoughts")]
    public Thoughts[] Thoughts { get; set; }

    [JsonPropertyName("data_points")]
    public string[] DataPoints { get => DataPointsContent?.Select(x => $"{x.Title}: {x.Content}").ToArray() ?? Array.Empty<string>(); }

    public string ThoughtsString { get => string.Join("\n", Thoughts.Select(x => $"{x.Title}: {x.Description}")); }
}


public record ResponseMessage
{
    public ResponseMessage(string role, string content)
    {
        this.Content = content;
        this.Role = role;
    }

    [JsonPropertyName("content")]
    public string Content { get; set; }

    [JsonPropertyName("role")]
    public string Role { get; set; }

}

public record ResponseChoice
{
    public ResponseChoice(int Index, ResponseMessage message, ResponseContext context, string CitationBaseUrl, string? followupQuestion)
    {
        this.Index = Index;
        this.Message = message;
        this.Context = context;
        this.CitationBaseUrl = CitationBaseUrl;
    }

    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("message")]
    public ResponseMessage Message { get; set; }

    [JsonPropertyName("context")]
    public ResponseContext Context { get; set; }

    [JsonPropertyName("citationBaseUrl")]
    public string CitationBaseUrl { get; set; }

    [JsonPropertyName("content_filter_results")]
    public ContentFilterResult? ContentFilterResult { get; set; }

}

public record ChatAppResponse(ResponseChoice[] Choices);

public record ChatAppResponseOrError(
    ResponseChoice[] Choices,
    string? Error = null);
