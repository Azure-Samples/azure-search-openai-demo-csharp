// Copyright (c) Microsoft. All rights reserved.

namespace Shared.Models;

public record SupportingContentRecord(string Title, string Content);

public record SupportingImageRecord(string Title, string Url);

public record ApproachResponse(
    string Answer,
    string? Thoughts,
    SupportingContentRecord[]? DataPoints, // title, content
    SupportingImageRecord[]? Images, // title, url
    string CitationBaseUrl,
    string? Error = null);
