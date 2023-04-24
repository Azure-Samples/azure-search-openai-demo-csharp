// Copyright (c) Microsoft. All rights reserved.

//const int MaxSectionLength = 1_000;
//const int SentenceSearchLimit = 100;
//const int SectionOverlap = 100;

using Microsoft.Extensions.FileSystemGlobbing;

s_rootCommand.SetHandler(
    async (context) =>
    {
        if (context.ParseResult.GetValueForOption(s_removeAll))
        {
            await RemoveBlobsAsync(context);
            await RemoveFromIndexAsync(context);
        }
        else
        {
            // if not remove
            // create_search_index

            context.Console.WriteLine("Processing files...");
            // glob on the files
            Matcher matcher = new();
            matcher.AddInclude(context.ParseResult.GetValueForArgument(s_files));

        }
    });

return await s_rootCommand.InvokeAsync(args);

static async ValueTask RemoveBlobsAsync(
    InvocationContext context, string? filename = null)
{
    if (context.ParseResult.GetValueForOption(s_verbose))
    {
        context.Console.WriteLine($"Removing blobs for '{filename ?? "all"}'");
    }

    //var blobService = new BlobServiceClient($"""
    //    https://{context.ParseResult.GetValueForOption(s_storageAccount)}.blob.core.windows.net
    //    """, );

    await ValueTask.CompletedTask;
}

static async ValueTask RemoveFromIndexAsync(InvocationContext context)
{
    if (context.ParseResult.GetValueForOption(s_verbose))
    {
        context.Console.WriteLine("");
    }

    await ValueTask.CompletedTask;
}

static string BlobNameFromFilePage(string filename, int page = 0) =>
    Path.GetExtension(filename).ToLower() is ".pdf"
        ? $"{Path.GetFileNameWithoutExtension(filename)}-{page}.pdf"
        : Path.GetFileName(filename);

static async ValueTask UploadBlobsAsync(InvocationContext context, string fileName)
{
    var blobService = new BlobServiceClient(
        new Uri($"https://{context.ParseResult.GetValueForOption(s_storageAccount)}.blob.core.windows.net"),
        DefaultCredential);

    var container =
        blobService.GetBlobContainerClient(context.GetArgValue(s_container));

    await container.CreateIfNotExistsAsync();

    if (Path.GetExtension(fileName).ToLower() is ".pdf")
    {
        using var reader = new PdfReader(fileName);
        var pdf = new PdfDocument(reader);

        for (int i = 1; i <= pdf.GetNumberOfPages(); i++)
        {
            var blobName = BlobNameFromFilePage(fileName, i - 1);
            if (context.ParseResult.GetValueForOption(s_verbose))
            {
                context.Console.WriteLine($"\tUploading blob for page {i} -> {blobName}");
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
