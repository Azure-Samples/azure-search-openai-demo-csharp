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

    internal async Task EmbedBlobAsync(Stream blobStream, string blobName)
    {
        try
        {
            var embeddingType = GetEmbeddingType();
            var embedService = _embedServiceFactory.GetEmbedService(embeddingType);

            var result = await embedService.EmbedBlobAsync(blobStream, blobName);

            // When successfully embedded, update the blobs metadata:
            // key: "Processed", value: embeddingType
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to embed: {Name}, error: {Message}", blobName, ex.Message);
        }
    }

    private static EmbeddingType GetEmbeddingType()
    {
        if (Environment.GetEnvironmentVariable("EMBEDDING_TYPE") is string type &&
            Enum.TryParse<EmbeddingType>(type, out EmbeddingType embeddingType))
        {
            return embeddingType;
        }

        return EmbeddingType.AzureSearch;
    }
}
