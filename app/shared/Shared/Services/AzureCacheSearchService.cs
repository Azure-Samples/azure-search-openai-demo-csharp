using Azure.AI.OpenAI;
using Azure.Identity;
using EmbedFunctions.Services;
using NRedisStack.RedisStackCommands;
using NRedisStack.Search;
using Shared.Models;

public class AzureCacheSearchService(string redisConnectionString, string indexName, string openAiEndpoint = "", string openAiEmbeddingDeployment = "") : ISearchService
{
    private readonly RedisConnection _connection = RedisConnection.InitializeAsync(redisConnectionString).Result;

    public async Task<SupportingContentRecord[]> QueryDocumentsAsync(
        string? query = null,
        float[]? embedding = null,
        RequestOverrides? overrides = null,
        CancellationToken cancellationToken = default)
    {
        if (embedding is null && query is null)
        {
            throw new ArgumentException("Embedding or query must be provided");
        }

        if (embedding is null)
        {
            try
            {
                var openAIClient = new OpenAIClient(new Uri(openAiEndpoint), new DefaultAzureCredential());
                var embeddingResponse = await openAIClient.GetEmbeddingsAsync(new EmbeddingsOptions(openAiEmbeddingDeployment, [query]));
                embedding = embeddingResponse.Value.Data.First().Embedding.ToArray();
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Failed to get embeddings: openAiEndpoint={openAiEndpoint} openAiEmbeddingDeployment={openAiEmbeddingDeployment} {e.Message}");
            }
        }

        var documentContents = string.Empty;
        var top = overrides?.Top ?? 3;
        var exclude_category = overrides?.ExcludeCategory;
        var filter = exclude_category == null ? string.Empty : $"category ne '{exclude_category}'";
        var useSemanticRanker = overrides?.SemanticRanker ?? false;
        var useSemanticCaptions = overrides?.SemanticCaptions ?? false;

        if (overrides?.RetrievalMode != RetrievalMode.Text)
        {
            var k = useSemanticRanker ? 50 : top;

        }

        byte[] queryVector = AzureCacheEmbedService.FloatArrayToByteArray(embedding);
        SearchResult searchResult = await SearchVectorIndexAsync(indexName, queryVector, top, "pdf");

        var sb = new List<SupportingContentRecord>();
        foreach (var doc in searchResult.Documents)
        {
            string sourcePage = string.Empty;
            string content = string.Empty;

            foreach (var item in doc.GetProperties())
            {
                if (item.Key == "content")
                {
                    content = item.Value.ToString().Replace('\r', ' ').Replace('\n', ' '); ;
                }
                if (item.Key == "sourcepage")
                {
                    sourcePage = item.Value.ToString();
                }
            }

            if(!string.IsNullOrEmpty(content) && !string.IsNullOrEmpty(sourcePage))
            {
                sb.Add(new SupportingContentRecord(sourcePage, content));
            }
        }
        return [.. sb];
    }


    private async Task<SearchResult> SearchVectorIndexAsync(string indexName, byte[] queryVector, int topK, string category)
    {
        // Construct the query
        var query = new NRedisStack.Search.Query($"@category:{category}=>[KNN {topK} @embedding $query_vector]")
            .AddParam("query_vector", queryVector)
            .SetSortBy("__embedding_score")
            .Dialect(2);

        // Execute the query
        return await _connection.BasicRetryAsync(async db => await db.FT().SearchAsync(indexName, query));
    }

    public async Task<SupportingImageRecord[]> QueryImagesAsync(
        string? query = null,
        float[]? embedding = null,
        RequestOverrides? overrides = null,
        CancellationToken cancellationToken = default)
    {
        var top = overrides?.Top ?? 3;
        var exclude_category = overrides?.ExcludeCategory;
        var filter = exclude_category == null ? string.Empty : $"category ne '{exclude_category}'";

        if (embedding is null)
        {
            throw new ArgumentException("embedding must be provided");
        }
        else
        {
            byte[] queryVector = AzureCacheEmbedService.FloatArrayToByteArray(embedding);
            SearchResult searchResult = await SearchVectorIndexAsync(indexName, queryVector, top, "image");

            var sb = new List<SupportingImageRecord>();
            foreach (var doc in searchResult.Documents)
            {
                string url = string.Empty;
                string name = string.Empty;

                foreach (var item in doc.GetProperties())
                {
                    if (item.Key == "content")
                    {
                        name = item.Value.ToString();
                    }
                    if (item.Key == "id")
                    {
                        url = item.Value.ToString();
                    }
                }

                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(url))
                {
                    sb.Add(new SupportingImageRecord(name, url));
                }
            }

            return [.. sb];
        }
    }
}
