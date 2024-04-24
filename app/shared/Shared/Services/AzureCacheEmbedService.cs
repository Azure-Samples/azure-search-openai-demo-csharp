// Copyright (c) Microsoft. All rights reserved.

using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Storage.Blobs;
using EmbedFunctions.Services;
using Microsoft.Extensions.Logging;
using Shared.Models;
using StackExchange.Redis;

public class AzureCacheEmbedService(
    string redisConnectionString,
    OpenAIClient openAIClient,
    string embeddingModelName,
    SearchClient searchClient,
    string searchIndexName,
    SearchIndexClient searchIndexClient,
    DocumentAnalysisClient documentAnalysisClient,
    BlobContainerClient corpusContainerClient,
    IComputerVisionService? computerVisionService = null,
    bool includeImageEmbeddingsField = false,
    ILogger<AzureSearchEmbedService>? logger = null) : IEmbedService
{
    private readonly AzureSearchEmbedService _azureSearchEmbedService = new(
        openAIClient,
        embeddingModelName,
        searchClient,
        searchIndexName,
        searchIndexClient,
        documentAnalysisClient,
        corpusContainerClient,
        computerVisionService,
        includeImageEmbeddingsField,
        logger);

    private readonly RedisConnection _connection = RedisConnection.InitializeAsync(redisConnectionString).Result;
    private static readonly int s_embeddingDimension = 1536;

    public async Task CreateSearchIndexAsync(string searchIndexName, CancellationToken ct = default)
    {
        try
        {
            await _connection.BasicRetryAsync(async db => await db.ExecuteAsync("FT.INFO", searchIndexName));
        }
        catch
        {
            int vectorDimension = computerVisionService is not null ? Math.Max(s_embeddingDimension, computerVisionService.Dimension) : s_embeddingDimension;
            await CreateVectorIndexAsync(searchIndexName, "doc", "id TEXT content TEXT category TEXT sourcepage TEXT sourcefile TEXT", "FLAT", "embedding", vectorDimension, "FLOAT32", "COSINE");
        }
    }

    private async Task<RedisResult> CreateVectorIndexAsync(string indexName, string prefix, string schema, string indexType, string vectorName, int vectorDimension, string type, string distanceMetric)
    {
        string indexCommand = $"{indexName} ON HASH PREFIX 1 {prefix}: SCHEMA {schema} {vectorName} VECTOR {indexType} 6 TYPE {type} DIM {vectorDimension} DISTANCE_METRIC {distanceMetric}";
        return await _connection.BasicRetryAsync(async db => await db.ExecuteAsync("FT.CREATE", indexCommand.Split(' ')));
    }

    public async Task<bool> EmbedImageBlobAsync(Stream imageStream, string imageUrl, string imageName, CancellationToken ct = default)
    {
        if (includeImageEmbeddingsField == false || computerVisionService is null)
        {
            throw new InvalidOperationException(
                "Computer Vision service is required to include image embeddings field, please enable GPT_4V support");
        }

        var embeddings = await computerVisionService.VectorizeImageAsync(imageUrl, ct);
        await IndexDocAsync(imageUrl, imageName, "image", "0", imageUrl, embeddings.vector);
        return true;
    }

    public async Task<bool> EmbedPDFBlobAsync(Stream pdfBlobStream, string blobName)
    {
        try
        {
            await EnsureSearchIndexAsync(searchIndexName);
            Console.WriteLine($"Embedding blob '{blobName}'");
            var pageMap = await _azureSearchEmbedService.GetDocumentTextAsync(pdfBlobStream, blobName);

            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(blobName);

            // Create corpus from page map and upload to blob
            // Corpus name format: fileName-{page}.txt
            foreach (var page in pageMap)
            {
                var corpusName = $"{fileNameWithoutExtension}-{page.Index}.txt";
                await _azureSearchEmbedService.UploadCorpusAsync(corpusName, page.Text);
            }

            var sections = _azureSearchEmbedService.CreateSections(pageMap, blobName);

            var infoLoggingEnabled = logger?.IsEnabled(LogLevel.Information);
            if (infoLoggingEnabled is true)
            {
                logger?.LogInformation("""
                Indexing sections from '{BlobName}' into search index '{SearchIndexName}'
                """,
                    blobName,
                    searchIndexName);
            }

            await IndexSectionsAsync(sections);

            return true;
        }
        catch (Exception exception)
        {
            logger?.LogError(
                exception, "Failed to embed blob '{BlobName}'", blobName);

            throw;
        }
    }

    public async Task EnsureSearchIndexAsync(string searchIndexName, CancellationToken ct = default)
    {
        await CreateSearchIndexAsync(searchIndexName, ct);
    }

    private async Task IndexSectionsAsync(IEnumerable<Section> sections)
    {
        foreach (var section in sections)
        {
            var embeddings = await openAIClient.GetEmbeddingsAsync(new Azure.AI.OpenAI.EmbeddingsOptions(embeddingModelName, [section.Content.Replace('\r', ' ')]));
            var embedding = embeddings.Value.Data.FirstOrDefault()?.Embedding.ToArray() ?? [];
            var sectionCategory = section.Category ?? "unknown";
            await IndexDocAsync(section.Id, section.Content, sectionCategory, section.SourcePage, section.SourceFile, embedding);
        }
    }

    private async Task IndexDocAsync(string id, string content, string category, string sourcepage, string sourcefile, float[] embedding)
    {
        await _connection.BasicRetryAsync(async db =>
                   db.HashSetAsync($"doc:{id}",
                              [
                    new HashEntry("id", id),
                    new HashEntry("content", content),
                    new HashEntry("category", category),
                    new HashEntry("sourcepage", sourcepage),
                    new HashEntry("sourcefile", sourcefile),
                    new HashEntry("embedding", FloatArrayToByteArray(embedding))
            ]).Status
                       );
    }

    public static byte[] FloatArrayToByteArray(float[] originalArray)
    {
        float[] floatArray = EnsureCorrectLength(originalArray, s_embeddingDimension);
        byte[] byteArray = new byte[floatArray.Length * 4];
        Buffer.BlockCopy(floatArray, 0, byteArray, 0, byteArray.Length);
        return byteArray;
    }

    private static float[] EnsureCorrectLength(float[] arr, int length)
    {
        if (arr.Length > length)
        {
            // Truncate the array if it is too long
            Array.Resize(ref arr, length);
        }
        else if (arr.Length < length)
        {
            // Fill the array with 0.0 if it is too short
            Array.Resize(ref arr, length);
            for (int i = arr.Length - 1; i >= 0; i--)
            {
                arr[i] = 0.0f;
            }
        }
        // Return the modified array
        return arr;
    }
}
