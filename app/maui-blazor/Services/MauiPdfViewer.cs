// Copyright (c) Microsoft. All rights reserved.

namespace MauiBlazor.Services;

public class MauiPdfViewer : IPdfViewer
{
    public async ValueTask ShowDocumentAsync(string name, string baseUrl)
    {
        await Browser.Default.OpenAsync($"{baseUrl}/{name}");
    }
}
