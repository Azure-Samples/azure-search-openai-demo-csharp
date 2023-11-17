// Copyright (c) Microsoft. All rights reserved.

namespace MauiBlazor.Services;

public class MauiPdfViewer : IPdfViewer
{
    public async Task ShowDocument(string name, string url)
    {
        await Application.Current.MainPage.DisplayAlert(name, $"Displaying PDF: {url}", "OK");
    }
}
