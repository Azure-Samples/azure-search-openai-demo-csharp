// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Components;

public sealed partial class SupportingContent
{
    internal static ParsedSupportingContentItem ParseSupportingContent(string item)
    {
        // Assumes the item starts with the file name followed by : and the content.
        // Example: "sdp_corporate.pdf: this is the content that follows".
        var parts = item.Split(":");
        var title = parts[0];

        if (parts is { Length: 2 })
        {
            return new ParsedSupportingContentItem(title, parts[1].Trim());
        }

        return new ParsedSupportingContentItem(title);
    }
}

internal readonly record struct ParsedSupportingContentItem(
    string Title,
    string? Content = null)
{
    internal bool IsEmpty =>
        string.IsNullOrWhiteSpace(Title) ||
        string.IsNullOrWhiteSpace(Content);
}
