// Copyright (c) Microsoft. All rights reserved.

internal readonly record struct Section
{
    public string Id { get; init; }
    public string Content { get; init; }
    public string? Category { get; init; }
    public string SourcePage { get; init; }
    public string SourceFile { get; init; }
}
