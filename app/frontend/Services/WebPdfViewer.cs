// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Services;

public class WebPdfViewer(IDialogService dialog, ISnackbar snackbar) : IPdfViewer
{
    public ValueTask ShowDocumentAsync(string name, string url)
    {
        var extension = Path.GetExtension(name);
        if (extension is ".pdf")
        {
            dialog.Show<PdfViewerDialog>(
            $"📄 {name}",
            new DialogParameters
            {
                [nameof(PdfViewerDialog.FileName)] = name,
                [nameof(PdfViewerDialog.BaseUrl)] = url.Replace($"/{name}", ""),
            },
            new DialogOptions
            {
                MaxWidth = MaxWidth.Large,
                FullWidth = true,
                CloseButton = true,
                CloseOnEscapeKey = true
            });
        }
        else if (extension is ".png" or ".jpg" or ".jpeg")
        {
            dialog.Show<ImageViewerDialog>(
            $"📄 {name}",
            new DialogParameters
            {
                [nameof(ImageViewerDialog.FileName)] = name,
                [nameof(ImageViewerDialog.Src)] = url,
            },
            new DialogOptions
            {
                MaxWidth = MaxWidth.Large,
                FullWidth = true,
                CloseButton = true,
                CloseOnEscapeKey = true
            });
        }
        else
        {
            snackbar.Add(
                $"Unsupported file type: '{extension}'",
                Severity.Error,
                static options =>
                {
                    options.ShowCloseIcon = true;
                    options.VisibleStateDuration = 10_000;
                });
        }

        return ValueTask.CompletedTask;
    }
}
