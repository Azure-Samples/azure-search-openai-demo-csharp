// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using Azure.Storage.Blobs;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace MinimalApi.Tests;
public class AzureSearchEmbedServiceTest
{
    [EnvironmentVariablesFact(
        "AZURE_SEARCH_SERVICE_ENDPOINT",
        "AZURE_OPENAI_ENDPOINT",
        "AZURE_OPENAI_EMBEDDING_DEPLOYMENT",
        "AZURE_STORAGE_BLOB_ENDPOINT")]
    public async Task EnsureSearchIndexWithoutImageEmbeddingsAsync()
    {
        var indexName = nameof(EnsureSearchIndexWithoutImageEmbeddingsAsync).ToLower();
        var openAIEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? throw new InvalidOperationException();
        var embeddingDeployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_DEPLOYMENT") ?? throw new InvalidOperationException();
        var azureSearchEndpoint = Environment.GetEnvironmentVariable("AZURE_SEARCH_SERVICE_ENDPOINT") ?? throw new InvalidOperationException();
        var blobEndpoint = Environment.GetEnvironmentVariable("AZURE_STORAGE_BLOB_ENDPOINT") ?? throw new InvalidOperationException();
        var blobContainer = "test";

        var azureCredential = new DefaultAzureCredential();
        var openAIClient = new OpenAIClient(new Uri(openAIEndpoint), azureCredential);
        var searchClient = new SearchClient(new Uri(azureSearchEndpoint), indexName, azureCredential);
        var searchIndexClient = new SearchIndexClient(new Uri(azureSearchEndpoint), azureCredential);
        var documentAnalysisClient = new DocumentAnalysisClient(new Uri(azureSearchEndpoint), azureCredential);
        var blobServiceClient = new BlobServiceClient(new Uri(blobEndpoint), azureCredential);

        var service = new AzureSearchEmbedService(
            openAIClient: openAIClient,
            embeddingModelName: embeddingDeployment,
            searchClient: searchClient,
            searchIndexName: indexName,
            searchIndexClient: searchIndexClient,
            documentAnalysisClient: documentAnalysisClient,
            corpusContainerClient: blobServiceClient.GetBlobContainerClient(blobContainer),
            computerVisionService: null,
            includeImageEmbeddingsField: false,
            logger: null);

        try
        {
            // check if index exists
            var existsAction = async () => await searchIndexClient.GetIndexAsync(indexName);
            await existsAction.Should().ThrowAsync<RequestFailedException>();
            await service.EnsureSearchIndexAsync(indexName);

            var response = await searchIndexClient.GetIndexAsync(indexName);
            var index = response.Value;
            index.Name.Should().Be(indexName);
            index.Fields.Count.Should().Be(6);
            index.Fields.Select(f => f.Name).Should().BeEquivalentTo(["id", "content", "category", "sourcepage", "sourcefile", "embedding"]);

            // embedding's dimension should be 1536
            var embeddingField = index.Fields.Single(f => f.Name == "embedding");
            embeddingField.IsSearchable.Should().BeTrue();
            embeddingField.VectorSearchDimensions.Should().Be(1536);
        }
        finally
        {
            await searchIndexClient.DeleteIndexAsync(indexName);
        }
    }

    [EnvironmentVariablesFact(
        "AZURE_SEARCH_SERVICE_ENDPOINT",
        "AZURE_OPENAI_ENDPOINT",
        "AZURE_OPENAI_EMBEDDING_DEPLOYMENT",
        "AZURE_STORAGE_BLOB_ENDPOINT")]
    public async Task EnsureSearchIndexWithImageEmbeddingsAsync()
    {
        var indexName = nameof(EnsureSearchIndexWithImageEmbeddingsAsync).ToLower();
        var openAIEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? throw new InvalidOperationException();
        var embeddingDeployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_DEPLOYMENT") ?? throw new InvalidOperationException();
        var azureSearchEndpoint = Environment.GetEnvironmentVariable("AZURE_SEARCH_SERVICE_ENDPOINT") ?? throw new InvalidOperationException();
        var blobEndpoint = Environment.GetEnvironmentVariable("AZURE_STORAGE_BLOB_ENDPOINT") ?? throw new InvalidOperationException();
        var blobContainer = "test";

        var azureCredential = new DefaultAzureCredential();
        var openAIClient = new OpenAIClient(new Uri(openAIEndpoint), azureCredential);
        var searchClient = new SearchClient(new Uri(azureSearchEndpoint), indexName, azureCredential);
        var searchIndexClient = new SearchIndexClient(new Uri(azureSearchEndpoint), azureCredential);
        var documentAnalysisClient = new DocumentAnalysisClient(new Uri(azureSearchEndpoint), azureCredential);
        var blobServiceClient = new BlobServiceClient(new Uri(blobEndpoint), azureCredential);
        var computerVisionService = Substitute.For<IComputerVisionService>();
        computerVisionService.Dimension.Returns(1024);

        var service = new AzureSearchEmbedService(
            openAIClient: openAIClient,
            embeddingModelName: embeddingDeployment,
            searchClient: searchClient,
            searchIndexName: indexName,
            searchIndexClient: searchIndexClient,
            documentAnalysisClient: documentAnalysisClient,
            corpusContainerClient: blobServiceClient.GetBlobContainerClient(blobContainer),
            computerVisionService: computerVisionService,
            includeImageEmbeddingsField: true,
            logger: null);

        try
        {
            // check if index exists
            var existsAction = async () => await searchIndexClient.GetIndexAsync(indexName);
            await existsAction.Should().ThrowAsync<RequestFailedException>();
            await service.EnsureSearchIndexAsync(indexName);

            var response = await searchIndexClient.GetIndexAsync(indexName);
            var index = response.Value;
            index.Name.Should().Be(indexName);
            index.Fields.Count.Should().Be(7);
            index.Fields.Select(f => f.Name).Should().BeEquivalentTo(["id", "content", "category", "sourcepage", "sourcefile", "embedding", "imageEmbedding"]);

            // imageEmbedding's dimension should be 1024
            var imageEmbeddingField = index.Fields.Single(f => f.Name == "imageEmbedding");
            imageEmbeddingField.IsSearchable.Should().BeTrue();
            imageEmbeddingField.VectorSearchDimensions.Should().Be(1024);
        }
        finally
        {
            await searchIndexClient.DeleteIndexAsync(indexName);
        }
    }

    [EnvironmentVariablesFact(
        "AZURE_SEARCH_SERVICE_ENDPOINT",
        "AZURE_OPENAI_ENDPOINT",
        "AZURE_OPENAI_EMBEDDING_DEPLOYMENT",
        "AZURE_FORMRECOGNIZER_SERVICE_ENDPOINT",
        "AZURE_STORAGE_BLOB_ENDPOINT")]
    public async Task GetDocumentTextTestAsync()
    {
        var indexName = nameof(GetDocumentTextTestAsync).ToLower();
        var openAIEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? throw new InvalidOperationException();
        var embeddingDeployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_DEPLOYMENT") ?? throw new InvalidOperationException();
        var azureSearchEndpoint = Environment.GetEnvironmentVariable("AZURE_SEARCH_SERVICE_ENDPOINT") ?? throw new InvalidOperationException();
        var blobEndpoint = Environment.GetEnvironmentVariable("AZURE_STORAGE_BLOB_ENDPOINT") ?? throw new InvalidOperationException();
        var azureFormRecognizerEndpoint = Environment.GetEnvironmentVariable("AZURE_FORMRECOGNIZER_SERVICE_ENDPOINT") ?? throw new InvalidOperationException();
        var blobContainer = "test";

        var azureCredential = new DefaultAzureCredential();
        var openAIClient = new OpenAIClient(new Uri(openAIEndpoint), azureCredential);
        var searchClient = new SearchClient(new Uri(azureSearchEndpoint), indexName, azureCredential);
        var searchIndexClient = new SearchIndexClient(new Uri(azureSearchEndpoint), azureCredential);
        var documentAnalysisClient = new DocumentAnalysisClient(new Uri(azureFormRecognizerEndpoint), azureCredential);
        var blobServiceClient = new BlobServiceClient(new Uri(blobEndpoint), azureCredential);

        var service = new AzureSearchEmbedService(
            openAIClient: openAIClient,
            embeddingModelName: embeddingDeployment,
            searchClient: searchClient,
            searchIndexName: indexName,
            searchIndexClient: searchIndexClient,
            documentAnalysisClient: documentAnalysisClient,
            corpusContainerClient: blobServiceClient.GetBlobContainerClient(blobContainer),
            computerVisionService: null,
            includeImageEmbeddingsField: false,
            logger: null);

        try
        {
            await service.EnsureSearchIndexAsync(indexName);
            var benefitOptionsPDFName = "Benefit_Options.pdf";
            var benefitOptionsPDFPath = Path.Combine("data", benefitOptionsPDFName);
            using var stream = File.OpenRead(benefitOptionsPDFPath);
            var pages = await service.GetDocumentTextAsync(stream, benefitOptionsPDFName);
            pages.Count.Should().Be(4);
        }
        finally
        {
            await searchIndexClient.DeleteIndexAsync(indexName);
        }
    }

    [EnvironmentVariablesFact(
        "AZURE_SEARCH_SERVICE_ENDPOINT",
        "AZURE_OPENAI_ENDPOINT",
        "AZURE_OPENAI_EMBEDDING_DEPLOYMENT",
        "AZURE_FORMRECOGNIZER_SERVICE_ENDPOINT",
        "AZURE_STORAGE_BLOB_ENDPOINT")]
    public async Task EmbedBlobWithoutImageEmbeddingTestAsync()
    {
        var indexName = nameof(EmbedBlobWithoutImageEmbeddingTestAsync).ToLower();
        var openAIEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? throw new InvalidOperationException();
        var embeddingDeployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_DEPLOYMENT") ?? throw new InvalidOperationException();
        var azureSearchEndpoint = Environment.GetEnvironmentVariable("AZURE_SEARCH_SERVICE_ENDPOINT") ?? throw new InvalidOperationException();
        var blobEndpoint = Environment.GetEnvironmentVariable("AZURE_STORAGE_BLOB_ENDPOINT") ?? throw new InvalidOperationException();
        var azureFormRecognizerEndpoint = Environment.GetEnvironmentVariable("AZURE_FORMRECOGNIZER_SERVICE_ENDPOINT") ?? throw new InvalidOperationException();
        var blobContainer = nameof(EmbedBlobWithoutImageEmbeddingTestAsync).ToLower();

        var azureCredential = new DefaultAzureCredential();
        var openAIClient = new OpenAIClient(new Uri(openAIEndpoint), azureCredential);
        var searchClient = new SearchClient(new Uri(azureSearchEndpoint), indexName, azureCredential);
        var searchIndexClient = new SearchIndexClient(new Uri(azureSearchEndpoint), azureCredential);
        var documentAnalysisClient = new DocumentAnalysisClient(new Uri(azureFormRecognizerEndpoint), azureCredential);
        var blobServiceClient = new BlobServiceClient(new Uri(blobEndpoint), azureCredential);
        var containerClient = blobServiceClient.GetBlobContainerClient(blobContainer);
        await containerClient.CreateIfNotExistsAsync();

        var service = new AzureSearchEmbedService(
            openAIClient: openAIClient,
            embeddingModelName: embeddingDeployment,
            searchClient: searchClient,
            searchIndexName: indexName,
            searchIndexClient: searchIndexClient,
            documentAnalysisClient: documentAnalysisClient,
            corpusContainerClient: containerClient,
            computerVisionService: null,
            includeImageEmbeddingsField: false,
            logger: null);

        try
        {
            await service.EnsureSearchIndexAsync(indexName);
            var benefitOptionsPDFName = "Benefit_Options.pdf";
            var benefitOptionsPDFPath = Path.Combine("data", benefitOptionsPDFName);
            using var stream = File.OpenRead(benefitOptionsPDFPath);
            var isSucceed = await service.EmbedPDFBlobAsync(stream, benefitOptionsPDFName);
            isSucceed.Should().BeTrue();

            // check if the document page is uploaded to blob
            var blobs = containerClient.GetBlobsAsync();
            var blobNames = blobs.Select(b => b.Name).ToListAsync();
            blobNames.Result.Count.Should().Be(4);
            blobNames.Result.Should().BeEquivalentTo([ "Benefit_Options-0.txt", "Benefit_Options-1.txt", "Benefit_Options-2.txt", "Benefit_Options-3.txt" ]);
        }
        finally
        {
            // clean up
            await searchIndexClient.DeleteIndexAsync(indexName);
            await blobServiceClient.DeleteBlobContainerAsync(blobContainer);
        }
    }

    [EnvironmentVariablesFact(
        "AZURE_SEARCH_SERVICE_ENDPOINT",
        "AZURE_OPENAI_ENDPOINT",
        "AZURE_OPENAI_EMBEDDING_DEPLOYMENT",
        "AZURE_FORMRECOGNIZER_SERVICE_ENDPOINT",
        "AZURE_STORAGE_BLOB_ENDPOINT")]
    public async Task EmbedImageBlobTestAsync()
    {
        var indexName = nameof(EmbedImageBlobTestAsync).ToLower();
        var openAIEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? throw new InvalidOperationException();
        var embeddingDeployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_DEPLOYMENT") ?? throw new InvalidOperationException();
        var azureSearchEndpoint = Environment.GetEnvironmentVariable("AZURE_SEARCH_SERVICE_ENDPOINT") ?? throw new InvalidOperationException();
        var blobEndpoint = Environment.GetEnvironmentVariable("AZURE_STORAGE_BLOB_ENDPOINT") ?? throw new InvalidOperationException();
        var azureFormRecognizerEndpoint = Environment.GetEnvironmentVariable("AZURE_FORMRECOGNIZER_SERVICE_ENDPOINT") ?? throw new InvalidOperationException();
        var blobContainer = nameof(EmbedImageBlobTestAsync).ToLower();

        var azureCredential = new DefaultAzureCredential();
        var openAIClient = new OpenAIClient(new Uri(openAIEndpoint), azureCredential);
        var searchClient = new SearchClient(new Uri(azureSearchEndpoint), indexName, azureCredential);
        var searchIndexClient = new SearchIndexClient(new Uri(azureSearchEndpoint), azureCredential);
        var documentAnalysisClient = new DocumentAnalysisClient(new Uri(azureFormRecognizerEndpoint), azureCredential);
        var blobServiceClient = new BlobServiceClient(new Uri(blobEndpoint), azureCredential);
        var containerClient = blobServiceClient.GetBlobContainerClient(blobContainer);
        await containerClient.CreateIfNotExistsAsync();
        var computerVisionService = Substitute.For<IComputerVisionService>();
        computerVisionService.Dimension.Returns(1024);
        computerVisionService.VectorizeImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new ImageEmbeddingResponse("test", new float[1024])));

        var service = new AzureSearchEmbedService(
            openAIClient: openAIClient,
            embeddingModelName: embeddingDeployment,
            searchClient: searchClient,
            searchIndexName: indexName,
            searchIndexClient: searchIndexClient,
            documentAnalysisClient: documentAnalysisClient,
            corpusContainerClient: containerClient,
            computerVisionService: computerVisionService,
            includeImageEmbeddingsField: true,
            logger: null);

        try
        {
            await service.EnsureSearchIndexAsync(indexName);
            var imageBlobName = "Financial Market Analysis Report 2023-04.png";
            var imagePath = Path.Combine("data", "imgs", imageBlobName);
            using var stream = File.OpenRead(imagePath);
            var client = containerClient.GetBlobClient(imageBlobName);
            await client.UploadAsync(stream, true);
            var url = client.Uri.AbsoluteUri;
            var isSucceed = await service.EmbedImageBlobAsync(stream, imageBlobName, url);
            isSucceed.Should().BeTrue();

            // check if the image is uploaded to blob
            var blobs = containerClient.GetBlobsAsync();
            var blobNames = blobs.Select(b => b.Name).ToListAsync();
            blobNames.Result.Count.Should().Be(1);
            blobNames.Result.Should().BeEquivalentTo([ imageBlobName ]);
        }
        finally
        {
            // clean up
            await searchIndexClient.DeleteIndexAsync(indexName);
            await blobServiceClient.DeleteBlobContainerAsync(blobContainer);
        }
    }
}
