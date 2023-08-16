// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Pages;

public sealed partial class Docs : IDisposable
{
    private MudForm _form = null!;
    private MudFileUpload<IReadOnlyList<IBrowserFile>> _fileUpload = null!;
    private Task _getDocumentsTask = null!;
    private bool _isLoadingDocuments = false;
    private string _filter = "";

    // Store a cancelation token that will be used to cancel if the user disposes of this component.
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly HashSet<DocumentResponse> _documents = new();

    [Inject]
    public required ApiClient Client { get; set; }

    private bool FilesSelected => _fileUpload is { Files.Count: > 0 };

    protected override void OnInitialized()
    {
        // Instead of awaiting this async enumerable here, let's capture it in a task
        // and start it in the background. This way, we can await it in the UI.
        _getDocumentsTask = GetDocumentsAsync();
    }

    private bool OnFilter(DocumentResponse document)
    {
        if (document is null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(_filter))
        {
            return true;
        }

        return document.Name.Contains(_filter, StringComparison.OrdinalIgnoreCase);
    }

    private async Task GetDocumentsAsync()
    {
        _isLoadingDocuments = true;

        try
        {
            var documents =
                await Client.GetDocumentsAsync(_cancellationTokenSource.Token)
                    .ToListAsync();

            foreach (var document in documents)
            {
                _documents.Add(document);
            }
        }
        finally
        {
            _isLoadingDocuments = false;
            StateHasChanged();
        }
    }

    private async Task SubmitFilesForUploadAsync()
    {
        await _form.Validate();

        if (_form.IsValid)
        {
            foreach (var file in _fileUpload.Files)
            {
                // TODO: implement this at as part of the Azure Function work.
                // We will call our Minimal API, and it will call our Functions.
            }
        }
    }

    public void Dispose() => _cancellationTokenSource.Cancel();
}
