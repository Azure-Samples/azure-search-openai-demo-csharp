// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.Functions.Worker;

public sealed class EmbeddingFunction
{
    private readonly EmbeddingAggregateService _embeddingAggregateService;
    private readonly ILogger<EmbeddingFunction> _logger;

    public EmbeddingFunction(
        EmbeddingAggregateService embeddingAggregateService,
        ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<EmbeddingFunction>();
        _embeddingAggregateService = embeddingAggregateService;
    }

    [Function(name: "embed-blob")]
    public Task EmbedAsync(
        [BlobTrigger(
            blobPath: "content/{name}",
            Connection = "AzureStorageAccountEndpoint")] Stream blobStream,
        string name,
        BlobClient client) => _embeddingAggregateService.EmbedBlobAsync(client, blobStream, blobName: name);
}
