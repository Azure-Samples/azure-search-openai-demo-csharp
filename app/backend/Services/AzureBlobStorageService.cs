﻿// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Services;

internal sealed class AzureBlobStorageService
{
    internal static DefaultAzureCredential DefaultCredential { get; } = new();

    private readonly BlobContainerClient _container;

    public AzureBlobStorageService(BlobContainerClient container) => _container = container;

    internal async Task<UploadDocumentsResponse> UploadFilesAsync(IEnumerable<IFormFile> files, CancellationToken cancellationToken)
    {
        var uploadedFiles = new List<string>();
        foreach (var file in files)
        {
            var fileName = file.FileName;

            await using var stream = file.OpenReadStream();

            using var documents = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
            for (int i = 0; i < documents.PageCount; i++)
            {
                var documentName = BlobNameFromFilePage(fileName, i);
                var blobClient = _container.GetBlobClient(documentName);
                if (await blobClient.ExistsAsync(cancellationToken))
                {
                    continue;
                }

                var tempFileName = Path.GetTempFileName();

                try
                {
                    using var document = new PdfDocument();
                    document.AddPage(documents.Pages[i]);
                    document.Save(tempFileName);

                    await using var tempStream = File.OpenRead(tempFileName);
                    await blobClient.UploadAsync(tempStream, new BlobHttpHeaders
                    {
                        ContentType = "application/pdf"
                    }, cancellationToken: cancellationToken);

                    uploadedFiles.Add(documentName);
                }
                finally
                {
                    File.Delete(tempFileName);
                }
            }
        }

        return new UploadDocumentsResponse(uploadedFiles.ToArray());
    }

    private static string BlobNameFromFilePage(string filename, int page = 0) =>
        Path.GetExtension(filename).ToLower() is ".pdf"
            ? $"{Path.GetFileNameWithoutExtension(filename)}-{page}.pdf"
            : Path.GetFileName(filename);
}
