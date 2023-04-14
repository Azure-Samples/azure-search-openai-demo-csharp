// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Components;

public sealed partial class PdfViewerDialog
{
    private bool _isLoaded = false;
    private string _pdfViewerVisibilityStyle => _isLoaded ? "display:default;" : "display:none;";

    [Parameter] public required string Title { get; set; }

    [CascadingParameter] public required MudDialogInstance Dialog { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await JavaScriptModule.RegisterIFrameLoadedAsync(
            "#pdf-viewer",
            () =>
            {
                _isLoaded = true;
                StateHasChanged();
            });
    }

    private void OnCloseClick() => Dialog.Close(DialogResult.Ok(true));
}
