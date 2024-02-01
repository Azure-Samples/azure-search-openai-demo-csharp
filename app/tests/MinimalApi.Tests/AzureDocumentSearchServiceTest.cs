// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Search.Documents;
using FluentAssertions;
using MinimalApi.Services;
using Shared.Models;

namespace MinimalApi.Tests;
public class AzureDocumentSearchServiceTest
{
    [EnvironmentVariablesFact("AZURE_SEARCH_INDEX", "AZURE_SEARCH_SERVICE_ENDPOINT")]
    public async Task QueryDocumentsTestTextOnlyAsync()
    {
        var index = Environment.GetEnvironmentVariable("AZURE_SEARCH_INDEX") ?? throw new InvalidOperationException();
        var endpoint = Environment.GetEnvironmentVariable("AZURE_SEARCH_SERVICE_ENDPOINT") ?? throw new InvalidOperationException();
        var searchClient = new SearchClient(new Uri(endpoint), index, new DefaultAzureCredential());
        var service = new AzureDocumentService(searchClient);

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

    [EnvironmentVariablesFact("AZURE_SEARCH_INDEX", "AZURE_SEARCH_SERVICE_ENDPOINT", "AZURE_OPENAI_ENDPOINT", "AZURE_OPENAI_EMBEDDING_DEPLOYMENT")]
    public async Task QueryDocumentsTestEmbeddingOnlyAsync()
    {
        var index = Environment.GetEnvironmentVariable("AZURE_SEARCH_INDEX") ?? throw new InvalidOperationException();
        var searchServceEndpoint = Environment.GetEnvironmentVariable("AZURE_SEARCH_SERVICE_ENDPOINT") ?? throw new InvalidOperationException();
        var openAiEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? throw new InvalidOperationException();
        var openAiEmbeddingDeployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_DEPLOYMENT") ?? throw new InvalidOperationException();
        var openAIClient = new OpenAIClient(new Uri(openAiEndpoint), new DefaultAzureCredential());
        var query = "What is included in my Northwind Health Plus plan that is not in standard?";
        var embeddingResponse = await openAIClient.GetEmbeddingsAsync(openAiEmbeddingDeployment, new EmbeddingsOptions(query));
        var embedding = embeddingResponse.Value.Data.First().Embedding;
        var searchClient = new SearchClient(new Uri(searchServceEndpoint), index, new DefaultAzureCredential());
        var service = new AzureDocumentService(searchClient);

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
}
