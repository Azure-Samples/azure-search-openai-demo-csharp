// Copyright (c) Microsoft. All rights reserved.

using Azure.AI.OpenAI;
using Azure.Identity;
using FluentAssertions;
using Shared.Models;

namespace MinimalApi.Tests;
public class AzureCacheDocumentSearchServiceTest
{
    private static readonly string s_acIndex = "gptkbindex";
    private static readonly string s_openAiEmbeddingDeployment = "embedding";
    private static readonly string s_cacheEndpoint = "";
    private static readonly string s_openAiEndpoint = "";
    private static readonly string s_computerVisionEndpoint = "";


    [Xunit.Fact]
    public async Task QueryDocumentsTestTextOnlyAsync()
    {
        Environment.SetEnvironmentVariable("AZURE_CACHE_INDEX", s_acIndex);
        Environment.SetEnvironmentVariable("AZURE_CACHE_SERVICE_ENDPOINT", s_cacheEndpoint);
        Environment.SetEnvironmentVariable("AZURE_OPENAI_ENDPOINT", s_openAiEndpoint);
        Environment.SetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_DEPLOYMENT", s_openAiEmbeddingDeployment);

        var index = Environment.GetEnvironmentVariable("AZURE_CACHE_INDEX") ?? throw new InvalidOperationException();
        var endpoint = Environment.GetEnvironmentVariable("AZURE_CACHE_SERVICE_ENDPOINT") ?? throw new InvalidOperationException();
        var openAiEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? throw new InvalidOperationException();
        var openAiEmbeddingDeployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_DEPLOYMENT") ?? throw new InvalidOperationException();
        var service = new AzureCacheSearchService(endpoint, index, openAiEndpoint, openAiEmbeddingDeployment);

        // query only
        var option = new RequestOverrides
        {
            RetrievalMode = RetrievalMode.Text,
            Top = 3,
            SemanticCaptions = true,
            SemanticRanker = true,
        };

        var query = "What is included in my Northwind Health Plus plan that is not in standard?";
        var records = await service.QueryDocumentsAsync(query, overrides: option);
        records.Count().Should().Be(3);
    }

    [Xunit.Fact]
    public async Task QueryDocumentsTestEmbeddingOnlyAsync()
    {
        Environment.SetEnvironmentVariable("AZURE_CACHE_INDEX", s_acIndex);
        Environment.SetEnvironmentVariable("AZURE_CACHE_SERVICE_ENDPOINT", s_cacheEndpoint);
        Environment.SetEnvironmentVariable("AZURE_OPENAI_ENDPOINT", s_openAiEndpoint);
        Environment.SetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_DEPLOYMENT", s_openAiEmbeddingDeployment);

        var index = Environment.GetEnvironmentVariable("AZURE_CACHE_INDEX") ?? throw new InvalidOperationException();
        var endpoint = Environment.GetEnvironmentVariable("AZURE_CACHE_SERVICE_ENDPOINT") ?? throw new InvalidOperationException();
        var openAiEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? throw new InvalidOperationException();
        var openAiEmbeddingDeployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_DEPLOYMENT") ?? throw new InvalidOperationException();
        var openAIClient = new OpenAIClient(new Uri(openAiEndpoint), new DefaultAzureCredential());
        var query = "What is included in my Northwind Health Plus plan that is not in standard?";
        var embeddingResponse = await openAIClient.GetEmbeddingsAsync(new EmbeddingsOptions(openAiEmbeddingDeployment, [query]));
        bool success = embeddingResponse.ToString() == "Status: 200, Value: Azure.AI.OpenAI.Embeddings";
        success.Should().BeTrue();
        var embedding = embeddingResponse.Value.Data.First().Embedding;
        var service = new AzureCacheSearchService(endpoint, index);

        // query only
        var option = new RequestOverrides
        {
            RetrievalMode = RetrievalMode.Vector,
            Top = 3,
            SemanticCaptions = true,
            SemanticRanker = true,
        };

        var records = await service.QueryDocumentsAsync(query: query, embedding: embedding.ToArray(), overrides: option);
        records.Count().Should().Be(3);
    }

    [Xunit.Fact]
    public async Task QueryImagesTestAsync()
    {
        Environment.SetEnvironmentVariable("AZURE_CACHE_INDEX", s_acIndex);
        Environment.SetEnvironmentVariable("AZURE_CACHE_SERVICE_ENDPOINT", s_cacheEndpoint);
        Environment.SetEnvironmentVariable("AZURE_COMPUTER_VISION_ENDPOINT", s_computerVisionEndpoint);

        var index = Environment.GetEnvironmentVariable("AZURE_CACHE_INDEX") ?? throw new InvalidOperationException();
        var endpoint = Environment.GetEnvironmentVariable("AZURE_CACHE_SERVICE_ENDPOINT") ?? throw new InvalidOperationException();
        var computerVisionEndpoint = Environment.GetEnvironmentVariable("AZURE_COMPUTER_VISION_ENDPOINT") ?? throw new InvalidOperationException();
        using var httpClient = new System.Net.Http.HttpClient();
        var computerVisionService = new AzureComputerVisionService(httpClient, computerVisionEndpoint, new DefaultAzureCredential());
        var service = new AzureCacheSearchService(endpoint, index);

        var query = "financial report";
        var queryEmbedding = await computerVisionService.VectorizeTextAsync(query);
        var option = new RequestOverrides
        {
            Top = 3,
        };

        var records = await service.QueryImagesAsync(query: query, embedding: queryEmbedding.vector, overrides: option);
        records.Count().Should().Be(3);
        records[0].Title.Should().Contain("Financial Market Analysis Report");
    }
}
