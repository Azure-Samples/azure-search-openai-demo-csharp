// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Services;

internal sealed class AzureBlobStorageService(BlobContainerClient container)
{
    internal static DefaultAzureCredential DefaultCredential { get; } = new();

    internal async Task<UploadDocumentsResponse> UploadFilesAsync(IEnumerable<IFormFile> files, CancellationToken cancellationToken)
    {
        try
        {
            List<string> uploadedFiles = [];
            foreach (var file in files)
            {
                var fileName = file.FileName;

                await using var stream = file.OpenReadStream();

                // if file is an image (end with .png, .jpg, .jpeg, .gif), upload it to blob storage
                if (Path.GetExtension(fileName).ToLower() is ".png" or ".jpg" or ".jpeg" or ".gif")
                {
                    var blobName = BlobNameFromFilePage(fileName);
                    var blobClient = container.GetBlobClient(blobName);
                    if (await blobClient.ExistsAsync(cancellationToken))
                    {
                        continue;
                    }

                    var url = blobClient.Uri.AbsoluteUri;
                    await using var fileStream = file.OpenReadStream();
                    await blobClient.UploadAsync(fileStream, new BlobHttpHeaders
                    {
                        ContentType = "image"
                    }, cancellationToken: cancellationToken);
                    uploadedFiles.Add(blobName);
                }
                else if (Path.GetExtension(fileName).ToLower() is ".pdf")
                {
                    using var documents = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
                    for (int i = 0; i < documents.PageCount; i++)
                    {
                        var documentName = BlobNameFromFilePage(fileName, i);
                        var blobClient = container.GetBlobClient(documentName);
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
            }

            if (uploadedFiles.Count is 0)
            {
                return UploadDocumentsResponse.FromError("""
                    No files were uploaded. Either the files already exist or the files are not PDFs or images.
                    """);
            }

            return new UploadDocumentsResponse([.. uploadedFiles]);
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception ex)
        {
            return UploadDocumentsResponse.FromError(ex.ToString());
        }
#pragma warning restore CA1031 // Do not catch general exception types
    }

    private static string BlobNameFromFilePage(string filename, int page = 0) =>
        Path.GetExtension(filename).ToLower() is ".pdf"
            ? $"{Path.GetFileNameWithoutExtension(filename)}-{page}.pdf"
            : Path.GetFileName(filename);
}
