// Copyright (c) Microsoft. All rights reserved.

//const int MaxSectionLength = 1_000;
//const int SentenceSearchLimit = 100;
//const int SectionOverlap = 100;

using Azure.AI.FormRecognizer.DocumentAnalysis;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Pdf.Canvas.Parser;
using System.Text;
using PrepareDocs;

s_rootCommand.SetHandler(
    async (context) =>
    {
        var options = GetAppOptions(context);
        if (options.RemoveAll)
        {
            await RemoveBlobsAsync(options);
            await RemoveFromIndexAsync(options);
        }
        else
        {
            /*
             
             if not args.remove:
        create_search_index()
    
    print(f"Processing files...")
    for filename in glob.glob(args.files):
        if args.verbose: print(f"Processing '{filename}'")
        if args.remove:
            remove_blobs(filename)
            remove_from_index(filename)
        elif args.removeall:
            remove_blobs(None)
            remove_from_index(None)
        else:
            if not args.skipblobs:
                upload_blobs(filename)
            page_map = get_document_text(filename)
            sections = create_sections(os.path.basename(filename), page_map)
            index_sections(os.path.basename(filename), sections)
             
             */




            // if not remove
            // create_search_index

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
                    await RemoveFromIndexAsync(options, match.Path);
                    continue;
                }

                if (options.SkipBlobs is false)
                {
                    await UploadBlobsAsync(options, match.Path);

                    var pageMap = await Get
                }
            }
        }
    });

return await s_rootCommand.InvokeAsync(args);

static List<(int, int, string)> GetDocumentText(
    AppOptions options, string filename)
{
    int offset = 0;
    List<(int, int, string)> page_map = new();
    if (options.LocalPdfParser)
    {
        PdfReader reader = new(filename);
        IList<PdfPage> pages = reader.GetPages();
        for (int page_num = 0; page_num < pages.Count; page_num++)
        {
            string page_text = PdfTextExtractor.GetTextFromPage(pages[page_num], new SimpleTextExtractionStrategy());
            page_map.Add((page_num, offset, page_text));
            offset += page_text.Length;
        }
    }
    else
    {
        if (options.Verbose)
        {
            options.Console.WriteLine($"Extracting text from '{filename}' using Azure Form Recognizer");
        }

        DocumentAnalysisClient form_recognizer_client = new(
            new Uri($"https://{options.FormRecognizerService}.cognitiveservices.azure.com/"), new AzureKeyCredential(formrecognizer_creds), new DocumentAnalysisClientOptions { Diagnostics = { IsLoggingContentEnabled = true } });
        using FileStream stream = File.OpenRead(filename);
        {
            AnalyzeDocumentOperation poller = form_recognizer_client.StartAnalyzeDocument("prebuilt-layout", stream);
             form_recognizer_results = poller.WaitForCompletionAsync().Result;
            IReadOnlyList<DocumentPage> pages = form_recognizer_results.Value.Pages;

            for (int page_num = 0; page_num < pages.Count; page_num++)
            {
                IReadOnlyList<DocumentTable> tables_on_page = form_recognizer_results.Value.Tables.Where(t => t.BoundingRegions[0].PageNumber == page_num + 1).ToList();

                // mark all positions of the table spans in the page
                int page_offset = pages[page_num].Spans[0].Offset;
                int page_length = pages[page_num].Spans[0].Length;
                int[] table_chars = Enumerable.Repeat(-1, page_length).ToArray();
                for (int table_id = 0; table_id < tables_on_page.Count; table_id++)
                {
                    foreach (DocumentSpan span in tables_on_page[table_id].Spans)
                    {
                        // replace all table spans with "table_id" in table_chars array
                        for (int i = 0; i < span.Length; i++)
                        {
                            int idx = span.Offset - page_offset + i;
                            if (idx >= 0 && idx < page_length)
                            {
                                table_chars[idx] = table_id;
                            }
                        }
                    }
                }

                // build page text by replacing characters in table spans with table html
                StringBuilder page_text = new();
                HashSet<int> added_tables = new();
                for (int idx = 0; idx < table_chars.Length; idx++)
                {
                    if (table_chars[idx] == -1)
                    {
                        page_text.Append(form_recognizer_results.Value.Content[page_offset + idx]);
                    }
                    else if (!added_tables.Contains(table_chars[idx]))
                    {
                        page_text.Append(TableToHtml(tables_on_page[table_chars[idx]]));
                        added_tables.Add(table_chars[idx]);
                    }
                }

                page_text.Append(' ');
                page_map.Add((page_num, offset, page_text.ToString()));
                offset += page_text.Length;
            }
        }
    }

    return page_map;
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
    AppOptions options, string? fileName = null)
{
    if (options.Verbose)
    {
        options.Console.WriteLine("");
    }

    await ValueTask.CompletedTask;
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

//static string TableToHtml(Table table)
//{
//    string table_html = "<table>";
//    List<Cell>[] rows = new List<Cell>[table.RowCount];
//    for (int i = 0; i < table.RowCount; i++)
//    {
//        rows[i] = table.Cells.Where(c => c.RowIndex == i).OrderBy(c => c.ColumnIndex).ToList();
//    }
//    foreach (List<Cell> row_cells in rows)
//    {
//        table_html += "<tr>";
//        foreach (Cell cell in row_cells)
//        {
//            string tag = (cell.Kind == "columnHeader" || cell.Kind == "rowHeader") ? "th" : "td";
//            string cell_spans = "";
//            if (cell.ColumnSpan > 1) cell_spans += $" colSpan={cell.ColumnSpan}";
//            if (cell.RowSpan > 1) cell_spans += $" rowSpan={cell.RowSpan}";
//            table_html += $"<{tag}{cell_spans}>{HttpUtility.HtmlEncode(cell.Content)}</{tag}>";
//        }
//        table_html += "</tr>";
//    }
//    table_html += "</table>";
//    return table_html;
//}
