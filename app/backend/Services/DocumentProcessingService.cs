using SharedWebApp.Models;

namespace MinimalApi.Services;

public interface IDocumentProcessingService
{
    Task<bool> ProcessDocumentAsync(DocumentQueueMessage message, CancellationToken cancellationToken = default);
}

public sealed class DocumentProcessingService : IDocumentProcessingService
{
    private readonly IEmbedService _embedService;
    private readonly IAzureBlobStorageService _blobService;

    public DocumentProcessingService(IEmbedService embedService, IAzureBlobStorageService blobService)
    {
        _embedService = embedService ?? throw new ArgumentNullException(nameof(embedService));
        _blobService = blobService ?? throw new ArgumentNullException(nameof(blobService));
    }

    public async Task<bool> ProcessDocumentAsync(DocumentQueueMessage message, CancellationToken cancellationToken = default)
    {
        await using var stream = await _blobService.OpenReadAsync(new Uri(message.BlobUri), cancellationToken);
        return await _embedService.EmbedPDFBlobAsync(stream, message.FileName);
    }
}
