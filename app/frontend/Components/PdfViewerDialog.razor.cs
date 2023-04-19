// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Components;

public sealed partial class PdfViewerDialog
{
    private bool _isLoaded = false;
    private Uri? _baseAddress;
    private string _pdfViewerVisibilityStyle => _isLoaded ? "display:default;" : "display:none;";

    [Inject] public required IHttpClientFactory Factory { get; set; }

    [Parameter] public required string Title { get; set; }

    [CascadingParameter] public required MudDialogInstance Dialog { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var client = Factory.CreateClient(typeof(ApiClient).Name);
        _baseAddress = client.BaseAddress;

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
