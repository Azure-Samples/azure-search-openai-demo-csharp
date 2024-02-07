﻿// Copyright (c) Microsoft. All rights reserved.


using EmbedFunctions.Services;

internal static partial class Program
{
    #region Private Fields

    private static BlobContainerClient? s_corpusContainerClient;
    private static BlobContainerClient? s_containerClient;
    private static DocumentAnalysisClient? s_documentClient;
    private static SearchIndexClient? s_searchIndexClient;
    private static SearchClient? s_searchClient;
    private static OpenAIClient? s_openAIClient;

    private static readonly SemaphoreSlim s_corpusContainerLock = new(1);
    private static readonly SemaphoreSlim s_containerLock = new(1);
    private static readonly SemaphoreSlim s_documentLock = new(1);
    private static readonly SemaphoreSlim s_searchIndexLock = new(1);
    private static readonly SemaphoreSlim s_searchLock = new(1);
    private static readonly SemaphoreSlim s_openAILock = new(1);
    private static readonly SemaphoreSlim s_embeddingLock = new(1);

    #endregion Private Fields

    #region Private Methods

    private static Task<AzureSearchEmbedService> GetAzureSearchEmbedService(AppOptions options) =>
        GetLazyClientAsync<AzureSearchEmbedService>(options, s_embeddingLock, async o =>
        {
            var openAIClient = await GetAzureOpenAIClientAsync(o);
            var embeddingModelName = o.EmbeddingModelName ?? throw new ArgumentNullException(nameof(o.EmbeddingModelName));
            var searchClient = await GetSearchClientAsync(o);
            var searchIndexName = o.SearchIndexName ?? throw new ArgumentNullException(nameof(o.SearchIndexName));
            var searchIndexClient = await GetSearchIndexClientAsync(o);
            var documentClient = await GetFormRecognizerClientAsync(o);
            var blobContainerClient = await GetCorpusBlobContainerClientAsync(o);

            return new AzureSearchEmbedService(openAIClient, embeddingModelName, searchClient, searchIndexName, searchIndexClient, documentClient, blobContainerClient, null);
        });

    #region Factory Methods

    // The Azure OpenAI service client.
    private static Task<OpenAIClient> GetAzureOpenAIClientAsync(AppOptions options) =>
       GetLazyClientAsync<OpenAIClient>(options, s_openAILock, async o =>
       {
           if (s_openAIClient is null)
           {
               var endpoint = o.AzureOpenAIServiceEndpoint;
               ArgumentNullException.ThrowIfNullOrEmpty(endpoint);

               s_openAIClient = new OpenAIClient(
                   new Uri(endpoint),
                   DefaultCredential);
           }

           await Task.CompletedTask;

           return s_openAIClient;
       });

    // Azure AI Search client that can be used to query an index and upload, merge, or delete documents.
    private static Task<SearchClient> GetSearchClientAsync(AppOptions options) =>
        GetLazyClientAsync<SearchClient>(options, s_searchLock, async o =>
        {
            if (s_searchClient is null)
            {
                var endpoint = o.SearchServiceEndpoint;
                ArgumentNullException.ThrowIfNullOrEmpty(endpoint);

                s_searchClient = new SearchClient(
                    new Uri(endpoint),
                    o.SearchIndexName,
                    DefaultCredential);
            }

            await Task.CompletedTask;

            return s_searchClient;
        });

    // Azure AI Search client that can be used to manage indexes on a Search service.
    private static Task<SearchIndexClient> GetSearchIndexClientAsync(AppOptions options) =>
        GetLazyClientAsync<SearchIndexClient>(options, s_searchIndexLock, static async o =>
        {
            if (s_searchIndexClient is null)
            {
                var endpoint = o.SearchServiceEndpoint;
                ArgumentNullException.ThrowIfNullOrEmpty(endpoint);

                s_searchIndexClient = new SearchIndexClient(
                    new Uri(endpoint),
                    DefaultCredential);
            }

            await Task.CompletedTask;

            return s_searchIndexClient;
        });

    // The client to use to connect to the Azure AI Document Intelligence to analyze information from documents and images and extract it into structured data.
    // It provides the ability to use prebuilt models to analyze receipts, business cards, invoices, to extract document content, and more.
    // It's also possible to extract fields from custom documents with models built on custom document types.
    private static Task<DocumentAnalysisClient> GetFormRecognizerClientAsync(AppOptions options) =>
        GetLazyClientAsync<DocumentAnalysisClient>(options, s_documentLock, static async o =>
        {
            if (s_documentClient is null)
            {
                var endpoint = o.FormRecognizerServiceEndpoint;
                ArgumentNullException.ThrowIfNullOrEmpty(endpoint);

                s_documentClient = new DocumentAnalysisClient(
                    new Uri(endpoint),
                    DefaultCredential,
                    new DocumentAnalysisClientOptions
                    {
                        Diagnostics =
                        {
                            IsLoggingContentEnabled = true
                        }
                    });
            }

            await Task.CompletedTask;

            return s_documentClient;
        });

    // The Blob Container Client allows you to manipulate Azure Storage containers and their blobs.
    private static Task<BlobContainerClient> GetCorpusBlobContainerClientAsync(AppOptions options) =>
        GetLazyClientAsync<BlobContainerClient>(options, s_corpusContainerLock, static async o =>
        {
            if (s_corpusContainerClient is null)
            {
                var endpoint = o.StorageServiceBlobEndpoint;
                ArgumentNullException.ThrowIfNullOrEmpty(endpoint);

                var blobService = new BlobServiceClient(
                    new Uri(endpoint),
                    DefaultCredential);

                s_corpusContainerClient = blobService.GetBlobContainerClient("corpus");

                await s_corpusContainerClient.CreateIfNotExistsAsync();
            }

            return s_corpusContainerClient;
        });

    private static Task<BlobContainerClient> GetBlobContainerClientAsync(AppOptions options) =>
        GetLazyClientAsync<BlobContainerClient>(options, s_containerLock, static async o =>
        {
            if (s_containerClient is null)
            {
                var endpoint = o.StorageServiceBlobEndpoint;
                ArgumentNullException.ThrowIfNullOrEmpty(endpoint);

                var blobContainerName = o.Container;
                ArgumentNullException.ThrowIfNullOrEmpty(blobContainerName);

                var blobService = new BlobServiceClient(
                    new Uri(endpoint),
                    DefaultCredential);

                s_containerClient = blobService.GetBlobContainerClient(blobContainerName);

                await s_containerClient.CreateIfNotExistsAsync();
            }

            return s_containerClient;
        });

    #endregion Factory Methods

    #region SemaphoreSlim Managment with factories

    private static async Task<TClient> GetLazyClientAsync<TClient>(
        AppOptions options,
        SemaphoreSlim locker,
        Func<AppOptions, Task<TClient>> factory)
    {
        await locker.WaitAsync();

        try
        {
            return await factory(options);
        }
        finally
        {
            locker.Release();
        }
    }

    #endregion SemaphoreSlim Managment with factories

    #endregion Private Methods
}
