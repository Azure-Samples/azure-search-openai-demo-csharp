// Copyright (c) Microsoft. All rights reserved.

using ClientApp.Components;

namespace ClientApp.Services;

public class WebPdfViewer(IDialogService dialog) : IPdfViewer
{
    public ValueTask ShowDocumentAsync(string name, string baseUrl)
    {
        dialog.Show<PdfViewerDialog>(
            $"📄 {name}",
            new DialogParameters
            {
                [nameof(PdfViewerDialog.FileName)] = name,
                [nameof(PdfViewerDialog.BaseUrl)] = baseUrl,
            },
            new DialogOptions
            {
                MaxWidth = MaxWidth.Large,
                FullWidth = true,
                CloseButton = true,
                CloseOnEscapeKey = true
            });

        return ValueTask.CompletedTask;
    }
}
