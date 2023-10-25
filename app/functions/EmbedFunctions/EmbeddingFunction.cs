// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.Functions.Worker;

public sealed class EmbeddingFunction(
    EmbeddingAggregateService embeddingAggregateService,
    ILoggerFactory loggerFactory)
{
    private readonly ILogger<EmbeddingFunction> _logger = loggerFactory.CreateLogger<EmbeddingFunction>();

    [Function(name: "embed-blob")]
    public Task EmbedAsync(
        [BlobTrigger(
            blobPath: "content/{name}",
            Connection = "AzureStorageAccountEndpoint")] Stream blobStream,
        string name,
        BlobClient client) => embeddingAggregateService.EmbedBlobAsync(client, blobStream, blobName: name);
}
