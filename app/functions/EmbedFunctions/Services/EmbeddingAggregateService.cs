// Copyright (c) Microsoft. All rights reserved.

namespace EmbedFunctions.Services;

public sealed class EmbeddingAggregateService(
    EmbedServiceFactory embedServiceFactory,
    BlobContainerClient client,
    ILogger<EmbeddingAggregateService> logger)
{
    internal async Task EmbedBlobAsync(Stream blobStream, string blobName)
    {
        try
        {
            var embeddingType = GetEmbeddingType();
            var embedService = embedServiceFactory.GetEmbedService(embeddingType);

            if (Path.GetExtension(blobName) is ".png" or ".jpg" or ".jpeg" or ".gif")
            {
                logger.LogInformation("Embedding image: {Name}", blobName);
                var result = await embedService.EmbedImageBlobAsync(blobStream, blobName, blobName);
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
            else if (Path.GetExtension(blobName) is ".pdf")
            {
                logger.LogInformation("Embedding pdf: {Name}", blobName);
                var result = await embedService.EmbedPDFBlobAsync(blobStream, blobName);

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
            else
            {
                throw new NotSupportedException("Unsupported file type.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to embed: {Name}, error: {Message}", blobName, ex.Message);
        }
    }

    private static EmbeddingType GetEmbeddingType() => Environment.GetEnvironmentVariable("EMBEDDING_TYPE") is string type &&
            Enum.TryParse<EmbeddingType>(type, out EmbeddingType embeddingType)
            ? embeddingType
            : EmbeddingType.AzureSearch;
}
