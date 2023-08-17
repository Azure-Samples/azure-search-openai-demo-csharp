﻿// Copyright (c) Microsoft. All rights reserved.

namespace EmbedFunctions.Services;

internal sealed partial class AzureSearchEmbedService : IEmbedService
{
    [GeneratedRegex("[^0-9a-zA-Z_-]")]
    private static partial Regex MatchInSetRegex();

    private readonly SearchClient _indexSectionClient;
    private readonly SearchIndexClient _searchIndexClient;
    private readonly DocumentAnalysisClient _documentAnalysisClient;
    private readonly BlobContainerClient _corpusContainerClient;
    private readonly ILogger<AzureSearchEmbedService> _logger;

    public AzureSearchEmbedService(
        SearchClient indexSectionClient,
        SearchIndexClient searchIndexClient,
        DocumentAnalysisClient documentAnalysisClient,
        BlobContainerClient corpusContainerClient,
        ILogger<AzureSearchEmbedService> logger)
    {
        _indexSectionClient = indexSectionClient;
        _searchIndexClient = searchIndexClient;
        _documentAnalysisClient = documentAnalysisClient;
        _corpusContainerClient = corpusContainerClient;
        _logger = logger;
    }

    public async Task<bool> EmbedBlobAsync(Stream blobStream, string blobName)
    {
        try
        {
            var searchIndexName = Environment.GetEnvironmentVariable(
                "AZURE_SEARCH_INDEX") ?? "gptkbindex";

            await EnsureSearchIndexAsync(searchIndexName);

            var pageMap = await GetDocumentTextAsync(blobStream, blobName);

            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(blobName);

            // Create corpus from page map and upload to blob
            // Corpus name format: fileName-{page}.txt
            foreach (var page in pageMap)
            {
                var corpusName = $"{fileNameWithoutExtension}-{page.Index}.txt";
                await UploadCorpusAsync(corpusName, page.Text);
            }

            var sections = CreateSections(pageMap, blobName);

            await IndexSectionsAsync(searchIndexName, sections, blobName);

            return true;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception, "Failed to embed blob '{BlobName}'", blobName);

            return false;
        }
    }

    private async Task EnsureSearchIndexAsync(string searchIndexName)
    {
        var indexNames = _searchIndexClient.GetIndexNamesAsync();
        await foreach (var page in indexNames.AsPages())
        {
            if (page.Values.Any(indexName => indexName == searchIndexName))
            {
                _logger.LogWarning(
                    "Search index '{SearchIndexName}' already exists", searchIndexName);
                return;
            }
        }

        var index = new SearchIndex(searchIndexName)
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

        _logger.LogInformation(
            "Creating '{searchIndexName}' search index", searchIndexName);

        await _searchIndexClient.CreateIndexAsync(index);
    }

    private async Task<IReadOnlyList<PageDetail>> GetDocumentTextAsync(Stream blobStream, string blobName)
    {
        _logger.LogInformation(
            "Extracting text from '{Blob}' using Azure Form Recognizer", blobName);

        AnalyzeDocumentOperation operation = _documentAnalysisClient.AnalyzeDocument(
            WaitUntil.Started, "prebuilt-layout", blobStream);

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

    private static string TableToHtml(DocumentTable table)
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

    private async Task UploadCorpusAsync(string corpusBlobName, string text)
    {
        var blobClient = _corpusContainerClient.GetBlobClient(corpusBlobName);
        if (await blobClient.ExistsAsync())
        {
            return;
        }

        _logger.LogInformation("Uploading corpus '{CorpusBlobName}'", corpusBlobName);

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        await blobClient.UploadAsync(stream, new BlobHttpHeaders
        {
            ContentType = "text/plain"
        });
    }

    private IEnumerable<Section> CreateSections(
        IReadOnlyList<PageDetail> pageMap, string blobName)
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

        _logger.LogInformation("Splitting '{BlobName}' into sections", blobName);

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
                Id: MatchInSetRegex().Replace($"{blobName}-{start}", "_").TrimStart('_'),
                Content: sectionText,
                SourcePage: BlobNameFromFilePage(blobName, FindPage(pageMap, start)),
                SourceFile: blobName);

            var lastTableStart = sectionText.LastIndexOf("<table", StringComparison.Ordinal);
            if (lastTableStart > 2 * SentenceSearchLimit && lastTableStart > sectionText.LastIndexOf("</table", StringComparison.Ordinal))
            {
                // If the section ends with an unclosed table, we need to start the next section with the table.
                // If table starts inside SentenceSearchLimit, we ignore it, as that will cause an infinite loop for tables longer than MaxSectionLength
                // If last table starts inside SectionOverlap, keep overlapping
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning("""
                        Section ends with unclosed table, starting next section with the
                        table at page {Offset} offset {Start} table start {LastTableStart}
                        """,
                        FindPage(pageMap, start),
                        start,
                        lastTableStart);
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
                Id: MatchInSetRegex().Replace($"{blobName}-{start}", "_").TrimStart('_'),
                Content: allText[start..end],
                SourcePage: BlobNameFromFilePage(blobName, FindPage(pageMap, start)),
                SourceFile: blobName);
        }
    }

    private static int FindPage(IReadOnlyList<PageDetail> pageMap, int offset)
    {
        var length = pageMap.Count;
        for (var i = 0; i < length - 1; i++)
        {
            if (offset >= pageMap[i].Offset && offset < pageMap[i + 1].Offset)
            {
                return i;
            }
        }

        return length - 1;
    }

    private static string BlobNameFromFilePage(string blobName, int page = 0) =>
        Path.GetExtension(blobName).ToLower() is ".pdf"
            ? $"{Path.GetFileNameWithoutExtension(blobName)}-{page}.pdf"
            : Path.GetFileName(blobName);

    private async Task IndexSectionsAsync(string searchIndexName, IEnumerable<Section> sections, string blobName)
    {
        var infoLoggingEnabled = _logger.IsEnabled(LogLevel.Information);
        if (infoLoggingEnabled)
        {
            _logger.LogInformation("""
                Indexing sections from '{BlobName}' into search index '{SearchIndexName}'
                """,
                blobName,
                searchIndexName);
        }

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
                IndexDocumentsResult result = await _indexSectionClient.IndexDocumentsAsync(batch);
                int succeeded = result.Results.Count(r => r.Succeeded);
                if (infoLoggingEnabled)
                {
                    _logger.LogInformation("""
                        Indexed {Count} sections, {Succeeded} succeeded
                        """,
                        batch.Actions.Count,
                        succeeded);
                }

                batch = new();
            }
        }

        if (batch is { Actions.Count: > 0 })
        {
            // Any remaining documents, batch create.
            var index = new SearchIndex($"index-{batch.Actions.Count}");
            IndexDocumentsResult result = await _indexSectionClient.IndexDocumentsAsync(batch);
            int succeeded = result.Results.Count(r => r.Succeeded);
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("""
                    Indexed {Count} sections, {Succeeded} succeeded
                    """,
                batch.Actions.Count,
                succeeded);
            }
        }
    }
}
