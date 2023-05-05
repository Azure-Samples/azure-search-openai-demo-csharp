// Copyright (c) Microsoft. All rights reserved.

internal static partial class Program
{
    private static BlobContainerClient? s_corpusContainerClient;
    private static BlobContainerClient? s_containerClient;
    private static DocumentAnalysisClient? s_documentClient;
    private static SearchIndexClient? s_searchIndexClient;
    private static SearchClient? s_searchClient;

    private static readonly SemaphoreSlim s_corpusContainerLock = new(1);
    private static readonly SemaphoreSlim s_containerLock = new(1);
    private static readonly SemaphoreSlim s_documentLock = new(1);
    private static readonly SemaphoreSlim s_searchIndexLock = new(1);
    private static readonly SemaphoreSlim s_searchLock = new(1);

    private static Task<BlobContainerClient> GetCorpusBlobContainerClientAsync(AppOptions options) =>
        GetLazyClientAsync<BlobContainerClient>(options, s_corpusContainerLock, async o =>
        {
            if (s_corpusContainerClient is null)
            {
                var endpoint = options.StorageServiceBlobEndpoint;
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
        GetLazyClientAsync<BlobContainerClient>(options, s_containerLock, async o =>
        {
            if (s_containerClient is null)
            {
                var endpoint = o.StorageServiceBlobEndpoint;
                ArgumentNullException.ThrowIfNullOrEmpty(endpoint);

                var blobService = new BlobServiceClient(
                    new Uri(endpoint),
                    DefaultCredential);

                var blobContainerName = o.Container;
                ArgumentNullException.ThrowIfNullOrEmpty(blobContainerName);

                s_containerClient = blobService.GetBlobContainerClient(blobContainerName);

                await s_containerClient.CreateIfNotExistsAsync();
            }

            return s_containerClient;
        });

    private static Task<DocumentAnalysisClient> GetFormRecognizerClientAsync(AppOptions options) =>
        GetLazyClientAsync<DocumentAnalysisClient>(options, s_documentLock, async o =>
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

    private static Task<SearchIndexClient> GetSearchIndexClientAsync(AppOptions options) =>
        GetLazyClientAsync<SearchIndexClient>(options, s_searchIndexLock, async o =>
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
}
