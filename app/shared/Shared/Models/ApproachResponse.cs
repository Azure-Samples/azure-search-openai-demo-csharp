// Copyright (c) Microsoft. All rights reserved.

namespace Shared.Models;

public record SupportingContentRecord(string Title, string Content);
public record ApproachResponse(
    string Answer,
    string? Thoughts,
    SupportingContentRecord[] DataPoints, // title, content
    string CitationBaseUrl,
    string? Error = null);
