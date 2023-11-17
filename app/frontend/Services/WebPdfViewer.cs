// Copyright (c) Microsoft. All rights reserved.

using ClientApp.Components;

namespace ClientApp.Services;

public class WebPdfViewer(IDialogService dialog) : IPdfViewer
{
    public Task ShowDocument(string name, string url)
    {
        dialog.Show<PdfViewerDialog>(
            $"📄 {name}",
            new DialogParameters
            {
                [nameof(PdfViewerDialog.FileName)] = name,
                [nameof(PdfViewerDialog.BaseUrl)] = url,
            },
            new DialogOptions
            {
                MaxWidth = MaxWidth.Large,
                FullWidth = true,
                CloseButton = true,
                CloseOnEscapeKey = true
            });

        return Task.CompletedTask;
    }
}
