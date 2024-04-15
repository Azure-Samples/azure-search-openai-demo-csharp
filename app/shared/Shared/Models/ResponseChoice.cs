// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;
using Azure.AI.OpenAI;

namespace Shared.Models;

public record SupportingContentRecord(string Title, string Content);

public record SupportingImageRecord(string Title, string Url);

public record DataPoints(
    [property: JsonPropertyName("text")] string[] Text)
{}

public record Thoughts(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("props")] (string, string)[]? Props = null)
{ }

public record ResponseContext(
    [property: JsonPropertyName("dataPointsContent")] SupportingContentRecord[]? DataPointsContent,
    [property: JsonPropertyName("dataPointsImages")] SupportingImageRecord[]? DataPointsImages,
    [property: JsonPropertyName("followup_questions")] string[] FollowupQuestions,
    [property: JsonPropertyName("thoughts")] Thoughts[] Thoughts)
{
    [JsonPropertyName("data_points")]
    public DataPoints DataPoints { get => new DataPoints(DataPointsContent?.Select(x => $"{x.Title}: {x.Content}").ToArray() ?? Array.Empty<string>()); }

    public string ThoughtsString { get => string.Join("\n", Thoughts.Select(x => $"{x.Title}: {x.Description}")); }
}


public record ResponseMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content)
{
}

public record ResponseChoice(
    [property: JsonPropertyName("index")] int Index,
    [property: JsonPropertyName("message")] ResponseMessage Message,
    [property: JsonPropertyName("context")] ResponseContext Context,
    [property: JsonPropertyName("citationBaseUrl")] string CitationBaseUrl)
{
    [JsonPropertyName("content_filter_results")]
    public ContentFilterResult? ContentFilterResult { get; set; }

}

public record ChatAppResponse(ResponseChoice[] Choices);

public record ChatAppResponseOrError(
    ResponseChoice[] Choices,
    string? Error = null);
