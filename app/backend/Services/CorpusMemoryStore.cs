// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Services;

public sealed class CorpusMemoryStore : IMemoryStore
{
    private readonly ILogger<CorpusMemoryStore> _logger;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly IMemoryStore _store = new VolatileMemoryStore();

    // TODO: Consider using the StringBuilderObjectPool approach for reusing builders in tight loops.
    // https://learn.microsoft.com/aspnet/core/performance/objectpool?view=aspnetcore-7.0
    public CorpusMemoryStore(BlobServiceClient blobServiceClient, ILogger<CorpusMemoryStore> logger) =>
        (_blobServiceClient, _logger) = (blobServiceClient, logger);

    public Task CreateCollectionAsync(
        string collectionName,
        CancellationToken cancel = default) =>
        _store.CreateCollectionAsync(collectionName, cancel);

    public Task DeleteCollectionAsync(
        string collectionName,
        CancellationToken cancel = default) =>
        _store.DeleteCollectionAsync(collectionName, cancel);

    public Task<bool> DoesCollectionExistAsync(
        string collectionName,
        CancellationToken cancel = default) =>
        _store.DoesCollectionExistAsync(collectionName, cancel);

    public Task<MemoryRecord?> GetAsync(
        string collectionName,
        string key,
        bool withEmbedding = false,
        CancellationToken cancel = default) =>
        _store.GetAsync(collectionName, key, withEmbedding, cancel);

    public IAsyncEnumerable<MemoryRecord> GetBatchAsync(
        string collectionName,
        IEnumerable<string> keys,
        bool withEmbeddings = false,
        CancellationToken cancel = default) =>
        _store.GetBatchAsync(collectionName, keys, withEmbeddings, cancel);

    public IAsyncEnumerable<string> GetCollectionsAsync(CancellationToken cancel = default) =>
        _store.GetCollectionsAsync(cancel);

    public Task<(MemoryRecord, double)?> GetNearestMatchAsync(
        string collectionName,
        Embedding<float> embedding,
        double minRelevanceScore = 0,
        bool withEmbedding = false,
        CancellationToken cancel = default) =>
        _store.GetNearestMatchAsync(collectionName, embedding, minRelevanceScore, withEmbedding, cancel);

    public IAsyncEnumerable<(MemoryRecord, double)> GetNearestMatchesAsync(
        string collectionName,
        Embedding<float> embedding,
        int limit,
        double minRelevanceScore = 0,
        bool withEmbeddings = false,
        CancellationToken cancel = default) =>
        _store.GetNearestMatchesAsync(collectionName, embedding, limit, minRelevanceScore, withEmbeddings, cancel);

    public async Task InitializeAsync()
    {
        _logger.LogInformation("Loading corpus ...");

        var blobContainerClient = _blobServiceClient.GetBlobContainerClient("corpus");
        var corpus = new List<CorpusRecord>();
        await foreach (var blob in blobContainerClient.GetBlobsAsync())
        {
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(blob.Name);
            var source = $"{fileNameWithoutExtension}.pdf";
            using var readStream = blobContainerClient.GetBlobClient(blob.Name).OpenRead();
            using var reader = new StreamReader(readStream);
            var content = await reader.ReadToEndAsync();

            // Split contents into short sentences
            var sentences = content.Split(new[] { '.', '?', '!' }, StringSplitOptions.RemoveEmptyEntries);
            var corpusIndex = 0;
            var sb = new StringBuilder();

            // Create corpus records based on sentences
            foreach (var sentence in sentences)
            {
                sb.Append(sentence);
                if (sb.Length > 256)
                {
                    var id = $"{source}+{corpusIndex++}";
                    corpus.Add(new CorpusRecord(id, source, sb.ToString()));
                    sb.Clear();
                }
            }
        }

        _logger.LogInformation("Load {Count} records into corpus", corpus.Count);
        _logger.LogInformation("Loading corpus into memory...");

        var embeddingService = new SentenceEmbeddingService(corpus);
        var collectionName = "knowledge";

        await _store.CreateCollectionAsync(collectionName);

        var embeddings = await embeddingService.GenerateEmbeddingsAsync(corpus.Select(c => c.Text).ToList());
        var memoryRecords =
            Enumerable.Zip(corpus, embeddings)
                .Select((tuple) =>
                {
                    var (corpusRecord, embedding) = tuple;
                    var metaData = new MemoryRecordMetadata(true, corpusRecord.Id, corpusRecord.Text, corpusRecord.Source, string.Empty, string.Empty);
                    var memoryRecord = new MemoryRecord(metaData, embedding, key: corpusRecord.Id);
                    return memoryRecord;
                });

        _ = await _store.UpsertBatchAsync(collectionName, memoryRecords).ToListAsync();
    }

    public Task RemoveAsync(
        string collectionName,
        string key,
        CancellationToken cancel = default) =>
        _store.RemoveAsync(collectionName, key, cancel);

    public Task RemoveBatchAsync(
        string collectionName,
        IEnumerable<string> keys,
        CancellationToken cancel = default) =>
        _store.RemoveBatchAsync(collectionName, keys, cancel);

    public Task<string> UpsertAsync(
        string collectionName,
        MemoryRecord record,
        CancellationToken cancel = default) =>
        _store.UpsertAsync(collectionName, record, cancel);

    public IAsyncEnumerable<string> UpsertBatchAsync(
        string collectionName,
        IEnumerable<MemoryRecord> records,
        CancellationToken cancel = default) =>
        _store.UpsertBatchAsync(collectionName, records, cancel);
}
