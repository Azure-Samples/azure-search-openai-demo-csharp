// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Pages;

public sealed partial class Index
{
    private readonly string[] _images = Enumerable.Range(0, 10)
        .Select(i => $"media/bing-generated-{i}.jpg")
        .ToArray();
}
