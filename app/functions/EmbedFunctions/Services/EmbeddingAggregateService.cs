// Copyright (c) Microsoft. All rights reserved.

namespace EmbedFunctions.Services;

public sealed class EmbeddingAggregateService
{
    private readonly EmbedServiceFactory _embedServiceFactory;
    private readonly ILogger<EmbeddingAggregateService> _logger;

    public EmbeddingAggregateService(
        EmbedServiceFactory embedServiceFactory,
        ILogger<EmbeddingAggregateService> logger)
    {
        _embedServiceFactory = embedServiceFactory;
        _logger = logger;
    }

    internal async Task EmbedBlobAsync(BlobClient client, Stream blobStream, string blobName)
    {
        try
        {
            var embeddingType = GetEmbeddingType();
            var embedService = _embedServiceFactory.GetEmbedService(embeddingType);

            var result = await embedService.EmbedBlobAsync(blobStream, blobName);

            var status = result switch
            {
                true => DocumentProcessingStatus.Succeeded,
                _ => DocumentProcessingStatus.Failed
            };

            await client.SetMetadataAsync(new Dictionary<string, string>
            {
                [nameof(DocumentProcessingStatus)] = status.ToString(),
                [nameof(EmbeddingType)] = embeddingType.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to embed: {Name}, error: {Message}", blobName, ex.Message);
        }
    }

    private static EmbeddingType GetEmbeddingType() => Environment.GetEnvironmentVariable("EMBEDDING_TYPE") is string type &&
            Enum.TryParse<EmbeddingType>(type, out EmbeddingType embeddingType)
            ? embeddingType
            : EmbeddingType.AzureSearch;
}
