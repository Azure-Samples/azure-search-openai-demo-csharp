// Copyright (c) Microsoft. All rights reserved.

namespace MauiBlazor.Services;

public class MauiPdfViewer : IPdfViewer
{
    public async ValueTask ShowDocumentAsync(string name, string baseUrl)
    {
        await Application.Current!.MainPage!.DisplayAlert(name, $"Displaying PDF: {baseUrl}/{name}", "OK");
    }
}
