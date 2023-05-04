// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Components;

public sealed partial class PdfViewerDialog
{
    private bool _isLoaded = false;
    private CitationResponse? _citationResponse;

    private string _pdfViewerVisibilityStyle => _isLoaded ? "display:default;" : "display:none;";

    [Inject] public required IHttpClientFactory Factory { get; set; }

    [Parameter] public required string Title { get; set; }

    [CascadingParameter] public required MudDialogInstance Dialog { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        var client = Factory.CreateClient(typeof(ApiClient).Name);

        var json = JsonSerializer.Serialize(
            new CitationRequest(Title),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        using var body = new StringContent(
            json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("api/citations", body);

        if (response.IsSuccessStatusCode)
        {
            _citationResponse = await response.Content.ReadFromJsonAsync<CitationResponse>();

            await JavaScriptModule.RegisterIFrameLoadedAsync(
                "#pdf-viewer",
                () =>
                {
                    _isLoaded = true;
                    StateHasChanged();
                });
        }

        await base.OnParametersSetAsync();
    }

    private void OnCloseClick() => Dialog.Close(DialogResult.Ok(true));
}
