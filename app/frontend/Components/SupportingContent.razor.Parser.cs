// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Components;

public sealed partial class SupportingContent
{
    internal ParsedSupportingContentItem ParseSupportingContent(string item)
    {
        // Assumes the item starts with the file name followed by : and the content.
        // Example: "sdp_corporate.pdf: this is the content that follows".
        var parts = item.Split(": ");
        var title = parts[0];
        var content = string.Join(": ", parts.Skip(1));

        return new ParsedSupportingContentItem(title, content);
    }
}

internal readonly record struct ParsedSupportingContentItem(
    string Title,
    string Content);
