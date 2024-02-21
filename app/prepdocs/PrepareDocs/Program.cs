// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics;

s_rootCommand.SetHandler(
    async (context) =>
    {
        var options = GetParsedAppOptions(context);
        if (options.RemoveAll)
        {
            await RemoveBlobsAsync(options);
            await RemoveFromIndexAsync(options);
        }
        else
        {
            var searchIndexName = options.SearchIndexName ?? throw new ArgumentNullException(nameof(options.SearchIndexName));
            var embedService = await GetAzureSearchEmbedService(options);
            await embedService.EnsureSearchIndexAsync(options.SearchIndexName);

            Matcher matcher = new();
            // From bash, the single quotes surrounding the path (to avoid expansion of the wildcard), are included in the argument value.
            matcher.AddInclude(options.Files.Replace("'", string.Empty));

            var results = matcher.Execute(
                new DirectoryInfoWrapper(
                    new DirectoryInfo(Directory.GetCurrentDirectory())));

            var files = results.HasMatches
                ? results.Files.Select(f => f.Path).ToArray()
                : Array.Empty<string>();

            context.Console.WriteLine($"Processing {files.Length} files...");

            var tasks = Enumerable.Range(0, files.Length)
                .Select(i =>
                {
                    var fileName = files[i];
                    return ProcessSingleFileAsync(options, fileName, embedService);
                });

            await Task.WhenAll(tasks);

            static async Task ProcessSingleFileAsync(AppOptions options, string fileName, IEmbedService embedService)
            {
                if (options.Verbose)
                {
                    options.Console.WriteLine($"Processing '{fileName}'");
                }

                if (options.Remove)
                {
                    await RemoveBlobsAsync(options, fileName);
                    await RemoveFromIndexAsync(options, fileName);
                    return;
                }

                if (options.SkipBlobs)
                {
                    return;
                }

                await UploadBlobsAndCreateIndexAsync(options, fileName, embedService);
            }
        }
    });

return await s_rootCommand.InvokeAsync(args);

static async ValueTask RemoveBlobsAsync(
    AppOptions options, string? fileName = null)
{
    if (options.Verbose)
    {
        options.Console.WriteLine($"Removing blobs for '{fileName ?? "all"}'");
    }

    var prefix = string.IsNullOrWhiteSpace(fileName)
        ? Path.GetFileName(fileName)
        : null;

    var getContainerClientTask = GetBlobContainerClientAsync(options);
    var getCorpusClientTask = GetCorpusBlobContainerClientAsync(options);
    var clientTasks = new[] { getContainerClientTask, getCorpusClientTask };

    await Task.WhenAll(clientTasks);

    foreach (var clientTask in clientTasks)
    {
        var client = await clientTask;
        await DeleteAllBlobsFromContainerAsync(client, prefix);
    }

    static async Task DeleteAllBlobsFromContainerAsync(BlobContainerClient client, string? prefix)
    {
        await foreach (var blob in client.GetBlobsAsync())
        {
            if (string.IsNullOrWhiteSpace(prefix) ||
                blob.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                await client.DeleteBlobAsync(blob.Name);
            }
        }
    };
}

static async ValueTask RemoveFromIndexAsync(
    AppOptions options, string? fileName = null)
{
    if (options.Verbose)
    {
        options.Console.WriteLine($"""
            Removing sections from '{fileName ?? "all"}' from search index '{options.SearchIndexName}.'
            """);
    }

    var searchClient = await GetSearchClientAsync(options);

    while (true)
    {
        var filter = (fileName is null) ? null : $"sourcefile eq '{Path.GetFileName(fileName)}'";

        var response = await searchClient.SearchAsync<SearchDocument>("",
            new SearchOptions
            {
                Filter = filter,
                Size = 1_000,
                IncludeTotalCount = true
            });

        var documentsToDelete = new List<SearchDocument>();
        await foreach (var result in response.Value.GetResultsAsync())
        {
            documentsToDelete.Add(new SearchDocument
            {
                ["id"] = result.Document["id"]
            });
        }

        if (documentsToDelete.Count == 0)
        {
            break;
        }
        Response<IndexDocumentsResult> deleteResponse =
            await searchClient.DeleteDocumentsAsync(documentsToDelete);

        if (options.Verbose)
        {
            Console.WriteLine($"""
                    Removed {deleteResponse.Value.Results.Count} sections from index
                """);
        }

        // It can take a few seconds for search results to reflect changes, so wait a bit
        await Task.Delay(TimeSpan.FromMilliseconds(2_000));
    }
}

static async ValueTask UploadBlobsAndCreateIndexAsync(
    AppOptions options, string fileName, IEmbedService embeddingService)
{
    var container = await GetBlobContainerClientAsync(options);

    // If it's a PDF, split it into single pages.
    if (Path.GetExtension(fileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
    {
        using var documents = PdfReader.Open(fileName, PdfDocumentOpenMode.Import);
        for (int i = 0; i < documents.PageCount; i++)
        {
            var documentName = BlobNameFromFilePage(fileName, i);
            var blobClient = container.GetBlobClient(documentName);
            if (await blobClient.ExistsAsync())
            {
                continue;
            }

            var tempFileName = Path.GetTempFileName();

            try
            {
                using var document = new PdfDocument();
                document.AddPage(documents.Pages[i]);
                document.Save(tempFileName);

                await using var stream = File.OpenRead(tempFileName);
                await blobClient.UploadAsync(stream, new BlobHttpHeaders
                {
                    ContentType = "application/pdf"
                });

                // revert stream position
                stream.Position = 0;

                await embeddingService.EmbedPDFBlobAsync(stream, documentName);
            }
            finally
            {
                File.Delete(tempFileName);
            }
        }
    }
    // if it's an img (end with .png/.jpg/.jpeg), upload it to blob storage and embed it.
    else if (Path.GetExtension(fileName).Equals(".png", StringComparison.OrdinalIgnoreCase) ||
        Path.GetExtension(fileName).Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
        Path.GetExtension(fileName).Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
    {
        using var stream = File.OpenRead(fileName);
        var blobName = BlobNameFromFilePage(fileName);
        var imageName = Path.GetFileNameWithoutExtension(blobName);
        var url = await UploadBlobAsync(fileName, blobName, container);
        await embeddingService.EmbedImageBlobAsync(stream, url, imageName);
    }
    else
    {
        var blobName = BlobNameFromFilePage(fileName);
        await UploadBlobAsync(fileName, blobName, container);
        await embeddingService.EmbedPDFBlobAsync(File.OpenRead(fileName), blobName);
    }
}

static async Task<string> UploadBlobAsync(string fileName, string blobName, BlobContainerClient container)
{
    var blobClient = container.GetBlobClient(blobName);
    var url = blobClient.Uri.AbsoluteUri;

    if (await blobClient.ExistsAsync())
    {
        return url;
    }

    var blobHttpHeaders = new BlobHttpHeaders
    {
        ContentType = GetContentType(fileName)
    };

    await using var fileStream = File.OpenRead(fileName);
    await blobClient.UploadAsync(fileStream, blobHttpHeaders);


    return url;
}

static string GetContentType(string fileName)
{
    var extension = Path.GetExtension(fileName);
    return extension switch
    {
        ".pdf" => "application/pdf",
        ".txt" => "text/plain",

        _ => "application/octet-stream"
    };
}

static string BlobNameFromFilePage(string filename, int page = 0) => Path.GetExtension(filename).ToLower() is ".pdf"
        ? $"{Path.GetFileNameWithoutExtension(filename)}-{page}.pdf"
        : Path.GetFileName(filename);

internal static partial class Program
{
    [GeneratedRegex("[^0-9a-zA-Z_-]")]
    private static partial Regex MatchInSetRegex();

    internal static DefaultAzureCredential DefaultCredential { get; } = new();
}
