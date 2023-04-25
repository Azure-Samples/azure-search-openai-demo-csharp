// Copyright (c) Microsoft. All rights reserved.

const int MaxSectionLength = 1_000;
const int SentenceSearchLimit = 100;
const int SectionOverlap = 100;

s_rootCommand.SetHandler(
    async (context) =>
    {
        var options = GetAppOptions(context);
        if (options.RemoveAll)
        {
            await RemoveBlobsAsync(options);
            await RemoveFromIndexAsync(context, options);
        }
        else
        {
            context.Console.WriteLine("Processing files...");

            Matcher matcher = new();
            matcher.AddInclude(context.ParseResult.GetValueForArgument(s_files));

            var results = matcher.Execute(
                new DirectoryInfoWrapper(
                    new DirectoryInfo(Directory.GetCurrentDirectory())));

            foreach (var match in results.Files)
            {
                if (options.Verbose)
                {
                    options.Console.WriteLine($"Processing '{match.Path}'");
                }

                if (options.Remove)
                {
                    await RemoveBlobsAsync(options, match.Path);
                    await RemoveFromIndexAsync(context, options, match.Path);
                    continue;
                }

                if (options.SkipBlobs is false)
                {
                    await UploadBlobsAsync(options, match.Path);

                    var pageMap = await GetDocumentTextAsync(options, match.Path);
                    var sections = CreateSections(match.Path, pageMap, options);
                    await IndexSectionsAsync(match.Path, sections, null, options);
                }
            }
        }
    });

return await s_rootCommand.InvokeAsync(args);

static async ValueTask IndexSectionsAsync(
    string fileName,
    IEnumerable<Section> sections,
    SearchClient searchClient,
    AppOptions options)
{
    if (options.Verbose)
    {
        options.Console.WriteLine($"""
            Indexing sections from '{fileName}' into search index '{options.Index}'
            """);
    }

    var i = 0;
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
                ["source_page"] = section.SourcePage,
                ["source_file"] = section.SourceFile
            }));

        if (++i % 1_000 is 0)
        {
            // Every one thousand documents, batch create.
            IndexDocumentsResult result = await searchClient.IndexDocumentsAsync(batch);
            int succeeded = result.Results.Count(r => r.Succeeded);
            if (options.Verbose)
            {
                options.Console.WriteLine($"""
                    \tIndexed {batch.Actions.Count} sections, {succeeded} succeeded
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
                \tIndexed {batch.Actions.Count} sections, {succeeded} succeeded
                """);
        }
    }
}

static async ValueTask<IReadOnlyList<PageDetail>> GetDocumentTextAsync(
    AppOptions options, string filename)
{
    int offset = 0;
    List<(int, int, string)> pageMap = new();
    if (options.LocalPdfParser)
    {
        using PdfReader reader = new(filename);
        using PdfDocument pdf = new(reader);

        for (var pageNumber = 0; pageNumber < pdf.GetNumberOfPages(); pageNumber++)
        {
            var pageText = PdfTextExtractor.GetTextFromPage(pdf.GetPage(pageNumber), new SimpleTextExtractionStrategy());
            pageMap.Add((pageNumber, offset, pageText));
            offset += pageText.Length;
        }
    }
    else
    {
        if (options.Verbose)
        {
            options.Console.WriteLine($"Extracting text from '{filename}' using Azure Form Recognizer");
        }

        ArgumentNullException.ThrowIfNullOrEmpty(options.FormRecognizerKey);

        var client = new DocumentAnalysisClient(
            new Uri($"https://{options.FormRecognizerService}.cognitiveservices.azure.com/"),
            new AzureKeyCredential(options.FormRecognizerKey),
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

        var results = await operation.WaitForCompletionAsync();
        var pages = results.Value.Pages;
        for (var i = 0; i < pages.Count; i++)
        {
            IReadOnlyList<DocumentTable> tablesOnPage =
                results.Value.Tables.Where(t => t.BoundingRegions[0].PageNumber == i + 1).ToList();

            // mark all positions of the table spans in the page
            int pageIndex = pages[i].Spans[0].Index;
            int pageLength = pages[i].Spans[0].Length;
            int[] tableChars = Enumerable.Repeat(-1, pageLength).ToArray();
            for (var tableId = 0; tableId < tablesOnPage.Count; tableId++)
            {
                foreach (DocumentSpan span in tablesOnPage[tableId].Spans)
                {
                    // replace all table spans with "tableId" in table_chars array
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

            // build page text by replacing characters in table spans with table html
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
            pageMap.Add((i, offset, pageText.ToString()));
            offset += pageText.Length;
        }
    }

    return pageMap.Select(pageDetails =>
        {
            var (index, offset, text) = pageDetails;
            return new PageDetail(index, offset, text);
        })
        .ToList()
        .AsReadOnly();
}

static async ValueTask RemoveBlobsAsync(
    AppOptions options, string? filename = null)
{
    if (options.Verbose)
    {
        options.Console.WriteLine($"Removing blobs for '{filename ?? "all"}'");
    }

    var blobService = new BlobServiceClient(
        new Uri($"https://{options.StorageAccount}.blob.core.windows.net"),
        DefaultCredential);

    await ValueTask.CompletedTask;
}

static async ValueTask RemoveFromIndexAsync(
    InvocationContext context,
    AppOptions options,
    string? fileName = null)
{
    if (options.Verbose)
    {
        options.Console.WriteLine("");
    }

    var searchClient = new SearchClient(
        new Uri($"https://{options.SearchService}.search.windows.net/"),
        options.Index,
        GetSearchCredential(context));

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
        Response<IndexDocumentsResult> deleteResponse = searchClient.DeleteDocuments(documentsToDelete);
        if (options.Verbose)
        {
            Console.WriteLine($"\tRemoved {deleteResponse.Value.Results.Count} sections from index");
        }

        // It can take a few seconds for search results to reflect changes, so wait a bit
        await Task.Delay(TimeSpan.FromMilliseconds(2_000));
    }
}

static string BlobNameFromFilePage(string filename, int page = 0) =>
    Path.GetExtension(filename).ToLower() is ".pdf"
        ? $"{Path.GetFileNameWithoutExtension(filename)}-{page}.pdf"
        : Path.GetFileName(filename);

static async ValueTask UploadBlobsAsync(
    AppOptions options, string fileName)
{
    var blobService = new BlobServiceClient(
        new Uri($"https://{options.StorageAccount}.blob.core.windows.net"),
        DefaultCredential);

    var container =
        blobService.GetBlobContainerClient(options.Container);

    await container.CreateIfNotExistsAsync();

    if (Path.GetExtension(fileName).ToLower() is ".pdf")
    {
        using var reader = new PdfReader(fileName);
        var pdf = new PdfDocument(reader);

        for (int i = 1; i <= pdf.GetNumberOfPages(); i++)
        {
            var blobName = BlobNameFromFilePage(fileName, i - 1);
            if (options.Verbose)
            {
                options.Console.WriteLine($"\tUploading blob for page {i} -> {blobName}");
            }

            using MemoryStream memoryStream = new();

            var writer = new PdfWriter(memoryStream);
            var target = new PdfDocument(writer);

            pdf.CopyPagesTo(i, i, target);

            await container.UploadBlobAsync(blobName, memoryStream);
        }
    }
    else
    {
        var blobName = BlobNameFromFilePage(fileName);
        await container.UploadBlobAsync(blobName, File.OpenRead(fileName));
    }
}

static string TableToHtml(DocumentTable table)
{
    var tableHtml = new StringBuilder("<table>");
    var rows = new List<DocumentTableCell>[table.RowCount];
    for (int i = 0; i < table.RowCount; i++)
    {
        rows[i] = table.Cells.Where(c => c.RowIndex == i).OrderBy(c => c.ColumnIndex).ToList();
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

static IEnumerable<Section> CreateSections(
    string fileName, IReadOnlyList<PageDetail> pageMap, AppOptions options)
{
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
        yield return new Section
        {
            Id = MatchInSetRegex().Replace($"{fileName}-{start}", "_"),
            Content = sectionText,
            Category = options.Category,
            SourcePage = BlobNameFromFilePage(fileName, FindPage(pageMap, start)),
            SourceFile = fileName
        };

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
        yield return new Section
        {

            Id = MatchInSetRegex().Replace($"{fileName}-{start}", "_"),
            Content = allText[start..end],
            Category = options.Category,
            SourcePage = BlobNameFromFilePage(fileName, FindPage(pageMap, start)),
            SourceFile = fileName
        };
    }
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
}
