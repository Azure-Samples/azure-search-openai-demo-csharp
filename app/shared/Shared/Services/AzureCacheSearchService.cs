using System.Collections.Generic;
using System.Text;
using Azure.AI.OpenAI;
using Azure.Identity;
using EmbedFunctions.Services;
using Shared.Models;
using StackExchange.Redis;

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
                throw new Exception($"Failed to get embeddings: openAiEndpoint={openAiEndpoint} openAiEmbeddingDeployment={openAiEmbeddingDeployment} {e.Message}");
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
        List<Dictionary<string, object>> searchResults = await SearchVectorIndexAsync(indexName, queryVector, top, "doc");

        var sb = new List<SupportingContentRecord>();
        foreach (var doc in searchResults)
        {
            string sourcePage = doc.GetValueOrDefault("sourcepage").ToString();
            string content = doc.GetValueOrDefault("content").ToString();
            content = content.Replace('\r', ' ').Replace('\n', ' ');
            sb.Add(new SupportingContentRecord(sourcePage, content));
        }

        return [.. sb];

    }

    private async Task<List<Dictionary<string, object>>> SearchVectorIndexAsync(string indexName, byte[] queryVector, int topK, string category)
    {
        // Construct the search command
        var searchCommand = new List<object>
        {
            indexName,
            //@category:{category}
            $"*=>[KNN {topK} @embedding $query_vector]",
            "PARAMS", "2",
            "query_vector", queryVector,
            "RETURN", "6", "__embedding_score", "id", "content", "category", "sourcepage", "sourcefile",
            "SORTBY", "__embedding_score",
            "DIALECT", "2"
        };

        // Execute the search command
        var searchResults = await _connection.BasicRetryAsync(async db => await db.ExecuteAsync("FT.SEARCH", searchCommand.ToArray()));

        // for debugging
        //StringBuilder sb = new StringBuilder();

        //for (int i = 0; i < searchResults.Length; i++)
        //{
        //    var a = searchResults[i];
        //    sb.AppendLine($"index {i}, {a.ToString()}, {a.Length}");
        //    for (int j = 0; j < searchResults[i].Length; j++)
        //    {
        //        var b = searchResults[i][j];
        //        sb.AppendLine($"   index {i}, {j}, {b.ToString()}, {b.Length}");
        //    }
        //}

        //string foo = sb.ToString();

        int numResults = (int)searchResults[0];

        List<Dictionary<string, object>> results = new List<Dictionary<string, object>>();

        for (int i = 1; i < searchResults.Length; i += 2)
        {
            string name = searchResults[i].ToString();
            var d = new Dictionary<string, object>();
            for (int j = 0; j < searchResults[i + 1].Length; j += 2)
            {
                var key = searchResults[i + 1][j];
                var value = searchResults[i + 1][j + 1];
                d.Add(key.ToString(), value);
            }
            results.Add(d);
        }

        return results;
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
            List<Dictionary<string, object>> searchResults = await SearchVectorIndexAsync(indexName, queryVector, top, "image");

            var sb = new List<SupportingImageRecord>();
            foreach (var doc in searchResults)
            {
                string name = doc.GetValueOrDefault("content").ToString();
                string url = doc.GetValueOrDefault("id").ToString();
                sb.Add(new SupportingImageRecord(name, url));
            }

            return [.. sb];
        }
    }
}
