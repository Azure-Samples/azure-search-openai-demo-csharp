// Copyright (c) Microsoft. All rights reserved.

using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;

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
            await CreateSearchIndexAsync(options);

            Matcher matcher = new();
            matcher.AddInclude(options.Files);

            var results = matcher.Execute(
                new DirectoryInfoWrapper(
                    new DirectoryInfo(Directory.GetCurrentDirectory())));

            var files = results.HasMatches
                ? results.Files.Select(f => f.Path).ToArray()
                : Array.Empty<string>();

            context.Console.WriteLine($"Processing {files.Length} files...");

            for (var i = 0; i < files.Length; ++ i)
            {
                var fileName = files[i];
                if (options.Verbose)
                {
                    options.Console.WriteLine($"Processing '{fileName}'");
                }

                if (options.Remove)
                {
                    await RemoveBlobsAsync(options, fileName);
                    await RemoveFromIndexAsync(options, fileName);
                    continue;
                }

                if (options.SkipBlobs is false)
                {
                    await UploadBlobsAsync(options, fileName);
                    var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                    var pageMap = await GetDocumentTextAsync(options, fileName);

                    // create corpus from page map and upload to blob
                    // corpus name format
                    // fileName-{page}.txt
                    foreach(var pages in pageMap)
                    {
                        var corpusName = $"{fileNameWithoutExtension}-{pages.Index}.txt";
                        await UploadCorpusAsync(options, corpusName, pages.Text);
                    }

                    var sections = CreateSections(options, pageMap, fileName);

                    await IndexSectionsAsync(options, sections, fileName);
                }
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

    var blobService = new BlobServiceClient(
        new Uri($"https://{options.StorageAccount}.blob.core.windows.net"),
        DefaultCredential);

    var container =
        blobService.GetBlobContainerClient(options.Container);

    await container.CreateIfNotExistsAsync();

    var prefix = string.IsNullOrWhiteSpace(fileName)
        ? Path.GetFileName(fileName)
        : null;

    await foreach (var blob in container.GetBlobsAsync())
    {
        if (string.IsNullOrWhiteSpace(prefix) ||
            blob.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            await container.DeleteBlobAsync(blob.Name);
        }
    }
}

static async ValueTask RemoveFromIndexAsync(
    AppOptions options, string? fileName = null)
{
    if (options.Verbose)
    {
        options.Console.WriteLine($"""
            Removing sections from '{fileName ?? "all"}' from search index '{options.Index}.'
            """);
    }

    var searchClient = new SearchClient(
        new Uri($"https://{options.SearchService}.search.windows.net/"),
        options.Index,
        DefaultCredential);

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

static async ValueTask CreateSearchIndexAsync(AppOptions options)
{
    var indexClient = new SearchIndexClient(
        new Uri($"https://{options.SearchService}.search.windows.net/"),
        DefaultCredential);

    var indices = await indexClient.GetIndexAsync(options.Index);
    if (indices is null or { HasValue: false } or { Value: null })
    {
        var index = new SearchIndex(options.Index)
        {
            Fields =
            {
                new SimpleField("id", SearchFieldDataType.String) { IsKey = true },
                new SearchableField("content") { AnalyzerName = "en.microsoft" },
                new SimpleField("category", SearchFieldDataType.String) { IsFacetable = true },
                new SimpleField("sourcepage", SearchFieldDataType.String) { IsFacetable = true },
                new SimpleField("sourcefile", SearchFieldDataType.String) { IsFacetable = true }
            },
            SemanticSettings = new SemanticSettings
            {
                Configurations =
                {
                    new SemanticConfiguration("default", new PrioritizedFields
                    {
                        ContentFields =
                        {
                            new SemanticField
                            {
                                FieldName = "content"
                            }
                        }
                    })
                }
            }
        };

        if (options.Verbose)
        {
            options.Console.WriteLine($"Creating '{options.Index}' search index");
        }

        await indexClient.CreateIndexAsync(index);
    }
    else if (options.Verbose)
    {
        options.Console.WriteLine($"Search index '{options.Index}' already exists");
    }
}

static async ValueTask UploadCorpusAsync(
       AppOptions options, string corpusName, string content)
{
    var blobService = new BlobServiceClient(
               new Uri($"https://{options.StorageAccount}.blob.core.windows.net"),
                      DefaultCredential);
    var container =
               blobService.GetBlobContainerClient("corpus");
    await container.CreateIfNotExistsAsync();
    var blob = container.GetBlobClient(corpusName);
    if (await blob.ExistsAsync())
    {
        return;
    }
    if (options.Verbose)
    {
        options.Console.WriteLine($"Uploading corpus '{corpusName}'");
    }

    var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
    await container.UploadBlobAsync(corpusName, stream);
}

static async ValueTask UploadBlobsAsync(
    AppOptions options, string fileName)
{
    var blobService = new BlobServiceClient(
        new Uri($"https://{options.StorageAccount}.blob.core.windows.net"),
        DefaultCredential);

    var container =
        blobService.GetBlobContainerClient(options.Container);

    await container.CreateIfNotExistsAsync();

    // if it's pdf file, split it into single pages
    if (Path.GetExtension(fileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
    {
        var documents = PdfReader.Open(fileName, PdfDocumentOpenMode.Import);
        for(int i = 0; i < documents.PageCount; i++)
        {
            var documentName = BlobNameFromFilePage(fileName, i);
            // check if the blob already exists
            var blob = container.GetBlobClient(documentName);
            if (await blob.ExistsAsync())
            {
                continue;
            }
            var document = new PdfDocument();
            document.AddPage(documents.Pages[i]);
            var tempFileName = Path.GetTempFileName();
            document.Save(tempFileName);
            using (var stream = File.OpenRead(tempFileName))
            {
                await container.UploadBlobAsync(documentName, stream);
            }
            File.Delete(tempFileName);
        }
    }
    else
    {
        await container.UploadBlobAsync(fileName, File.OpenRead(fileName));
    }
}

static async ValueTask<IReadOnlyList<PageDetail>> GetDocumentTextAsync(
    AppOptions options, string filename)
{
    if (options.Verbose)
    {
        options.Console.WriteLine($"Extracting text from '{filename}' using Azure Form Recognizer");
    }

    var client = new DocumentAnalysisClient(
        new Uri($"https://{options.FormRecognizerService}.cognitiveservices.azure.com/"),
        DefaultCredential,
        new DocumentAnalysisClientOptions
        {
            Diagnostics =
            {
                IsLoggingContentEnabled = true
            }
        });

    using FileStream stream = File.OpenRead(filename);

    AnalyzeDocumentOperation operation = client.AnalyzeDocument(
        WaitUntil.Started, "prebuilt-layout", stream);

    var offset = 0;
    List<PageDetail> pageMap = new();

    var results = await operation.WaitForCompletionAsync();
    var pages = results.Value.Pages;
    for (var i = 0; i < pages.Count; i++)
    {
        IReadOnlyList<DocumentTable> tablesOnPage =
            results.Value.Tables.Where(t => t.BoundingRegions[0].PageNumber == i + 1).ToList();

        // Mark all positions of the table spans in the page
        int pageIndex = pages[i].Spans[0].Index;
        int pageLength = pages[i].Spans[0].Length;
        int[] tableChars = Enumerable.Repeat(-1, pageLength).ToArray();
        for (var tableId = 0; tableId < tablesOnPage.Count; tableId++)
        {
            foreach (DocumentSpan span in tablesOnPage[tableId].Spans)
            {
                // Replace all table spans with "tableId" in tableChars array
                for (var j = 0; j < span.Length; j++)
                {
                    int index = span.Index - pageIndex + j;
                    if (index >= 0 && index < pageLength)
                    {
                        tableChars[index] = tableId;
                    }
                }
            }
        }

        // Build page text by replacing characters in table spans with table HTML
        StringBuilder pageText = new();
        HashSet<int> addedTables = new();
        for (int j = 0; j < tableChars.Length; j++)
        {
            if (tableChars[j] == -1)
            {
                pageText.Append(results.Value.Content[pageIndex + j]);
            }
            else if (!addedTables.Contains(tableChars[j]))
            {
                pageText.Append(TableToHtml(tablesOnPage[tableChars[j]]));
                addedTables.Add(tableChars[j]);
            }
        }

        pageText.Append(' ');
        pageMap.Add(new PageDetail(i, offset, pageText.ToString()));
        offset += pageText.Length;
    }

    return pageMap.AsReadOnly();
}

static IEnumerable<Section> CreateSections(
    AppOptions options, IReadOnlyList<PageDetail> pageMap, string fileName)
{
    const int MaxSectionLength = 1_000;
    const int SentenceSearchLimit = 100;
    const int SectionOverlap = 100;

    var sentenceEndings = new[] { '.', '!', '?' };
    var wordBreaks = new[] { ',', ';', ':', ' ', '(', ')', '[', ']', '{', '}', '\t', '\n' };
    var allText = string.Concat(pageMap.Select(p => p.Text));
    var length = allText.Length;
    var start = 0;
    var end = length;

    if (options.Verbose)
    {
        options.Console.WriteLine($"Splitting '{fileName}' into sections");
    }

    while (start + SectionOverlap < length)
    {
        var lastWord = -1;
        end = start + MaxSectionLength;

        if (end > length)
        {
            end = length;
        }
        else
        {
            // Try to find the end of the sentence
            while (end < length && (end - start - MaxSectionLength) < SentenceSearchLimit && !sentenceEndings.Contains(allText[end]))
            {
                if (wordBreaks.Contains(allText[end]))
                {
                    lastWord = end;
                }
                end++;
            }

            if (end < length && !sentenceEndings.Contains(allText[end]) && lastWord > 0)
            {
                end = lastWord; // Fall back to at least keeping a whole word
            }
        }

        if (end < length)
        {
            end++;
        }

        // Try to find the start of the sentence or at least a whole word boundary
        lastWord = -1;
        while (start > 0 && start > end - MaxSectionLength -
            (2 * SentenceSearchLimit) && !sentenceEndings.Contains(allText[start]))
        {
            if (wordBreaks.Contains(allText[start]))
            {
                lastWord = start;
            }
            start--;
        }

        if (!sentenceEndings.Contains(allText[start]) && lastWord > 0)
        {
            start = lastWord;
        }
        if (start > 0)
        {
            start++;
        }

        var sectionText = allText[start..end];

        yield return new Section(
            Id: MatchInSetRegex().Replace($"{fileName}-{start}", "_").TrimStart('_'),
            Content: sectionText,
            SourcePage: BlobNameFromFilePage(fileName, FindPage(pageMap, start)),
            SourceFile: fileName,
            Category: options.Category);

        var lastTableStart = sectionText.LastIndexOf("<table", StringComparison.Ordinal);
        if (lastTableStart > 2 * SentenceSearchLimit && lastTableStart > sectionText.LastIndexOf("</table", StringComparison.Ordinal))
        {
            // If the section ends with an unclosed table, we need to start the next section with the table.
            // If table starts inside SentenceSearchLimit, we ignore it, as that will cause an infinite loop for tables longer than MaxSectionLength
            // If last table starts inside SectionOverlap, keep overlapping
            if (options.Verbose)
            {
                options.Console.WriteLine($"""
                    Section ends with unclosed table, starting next section with the
                    table at page {FindPage(pageMap, start)} offset {start} table start {lastTableStart}
                    """);
            }

            start = Math.Min(end - SectionOverlap, start + lastTableStart);
        }
        else
        {
            start = end - SectionOverlap;
        }
    }

    if (start + SectionOverlap < end)
    {
        yield return new Section(
            Id: MatchInSetRegex().Replace($"{fileName}-{start}", "_").TrimStart('_'),
            Content: allText[start..end],
            SourcePage: BlobNameFromFilePage(fileName, FindPage(pageMap, start)),
            SourceFile: fileName,
            Category: options.Category);
    }
}

static async ValueTask IndexSectionsAsync(
    AppOptions options,
    IEnumerable<Section> sections,
    string fileName)
{
    if (options.Verbose)
    {
        options.Console.WriteLine($"""
            Indexing sections from '{fileName}' into search index '{options.Index}'
            """);
    }

    var searchClient = new SearchClient(
        new Uri($"https://{options.SearchService}.search.windows.net"),
        options.Index,
        DefaultCredential);

    var iteration = 0;
    var batch = new IndexDocumentsBatch<SearchDocument>();
    foreach (var section in sections)
    {
        batch.Actions.Add(new IndexDocumentsAction<SearchDocument>(
            IndexActionType.MergeOrUpload,
            new SearchDocument
            {
                ["id"] = section.Id,
                ["content"] = section.Content,
                ["category"] = section.Category,
                ["sourcepage"] = section.SourcePage,
                ["sourcefile"] = section.SourceFile
            }));

        iteration++;
        if (iteration % 1_000 is 0)
        {
            // Every one thousand documents, batch create.
            IndexDocumentsResult result = await searchClient.IndexDocumentsAsync(batch);
            int succeeded = result.Results.Count(r => r.Succeeded);
            if (options.Verbose)
            {
                options.Console.WriteLine($"""
                        Indexed {batch.Actions.Count} sections, {succeeded} succeeded
                    """);
            }

            batch = new();
        }
    }

    if (batch is { Actions.Count: > 0 })
    {
        // Any remaining documents, batch create.
        var index = new SearchIndex($"index-{batch.Actions.Count}");
        IndexDocumentsResult result = await searchClient.IndexDocumentsAsync(batch);
        int succeeded = result.Results.Count(r => r.Succeeded);
        if (options.Verbose)
        {
            options.Console.WriteLine($"""
                    Indexed {batch.Actions.Count} sections, {succeeded} succeeded
                """);
        }
    }
}

static string BlobNameFromFilePage(string filename, int page = 0) =>
    Path.GetExtension(filename).ToLower() is ".pdf"
        ? $"{Path.GetFileNameWithoutExtension(filename)}-{page}.pdf"
        : Path.GetFileName(filename);

static string TableToHtml(DocumentTable table)
{
    var tableHtml = new StringBuilder("<table>");
    var rows = new List<DocumentTableCell>[table.RowCount];
    for (int i = 0; i < table.RowCount; i++)
    {
        rows[i] = table.Cells.Where(c => c.RowIndex == i)
            .OrderBy(c => c.ColumnIndex)
            .ToList();
    }

    foreach (var rowCells in rows)
    {
        tableHtml.Append("<tr>");
        foreach (DocumentTableCell cell in rowCells)
        {
            var tag = (cell.Kind == "columnHeader" || cell.Kind == "rowHeader") ? "th" : "td";
            var cellSpans = string.Empty;
            if (cell.ColumnSpan > 1)
            {
                cellSpans += $" colSpan='{cell.ColumnSpan}'";
            }

            if (cell.RowSpan > 1)
            {
                cellSpans += $" rowSpan='{cell.RowSpan}'";
            }

            tableHtml.AppendFormat(
                "<{0}{1}>{2}</{0}>", tag, cellSpans, WebUtility.HtmlEncode(cell.Content));
        }

        tableHtml.Append("</tr>");
    }

    tableHtml.Append("</table>");

    return tableHtml.ToString();
}

static int FindPage(IReadOnlyList<PageDetail> pageMap, int offset)
{
    var length = pageMap.Count;
    for (var i = 0; i < length - 1; i++)
    {
        if (offset >= pageMap[i].Index && offset < pageMap[i + 1].Index)
        {
            return i;
        }
    }

    return length - 1;
}

internal static partial class Program
{
    [GeneratedRegex("[^0-9a-zA-Z_-]")]
    private static partial Regex MatchInSetRegex();

    internal static DefaultAzureCredential DefaultCredential { get; } = new();
}
