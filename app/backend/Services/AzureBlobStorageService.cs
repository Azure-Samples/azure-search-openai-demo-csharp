// Copyright (c) Microsoft. All rights reserved.

using Azure;

namespace MinimalApi.Services;

public interface IAzureBlobStorageService
{
    Task<UploadDocumentsResponse> UploadFilesAsync(
        IEnumerable<IFormFile> files,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a single file to {container}/{prefix}/{originalFileName or uniqueName} and returns the blob URI.
    /// If the target name exists, a short suffix is added to avoid overwriting.
    /// </summary>
    Task<Uri> UploadFileAsync(IFormFile file, string? prefix = null, CancellationToken cancellationToken = default);
}

internal sealed class AzureBlobStorageService(BlobContainerClient container) : IAzureBlobStorageService
{
    internal static DefaultAzureCredential DefaultCredential { get; } = new();

    public async Task<UploadDocumentsResponse> UploadFilesAsync(IEnumerable<IFormFile> files, CancellationToken cancellationToken)
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

    /// <summary>
    /// Uploads a single file. For PDFs, uploads the file as-is (no page-splitting).
    /// </summary>
    public async Task<Uri> UploadFileAsync(IFormFile file, string? prefix = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        var originalName = Path.GetFileName(file.FileName);
        var ext = Path.GetExtension(originalName).ToLowerInvariant();
        var basePath = string.IsNullOrWhiteSpace(prefix) ? "" : prefix!.Trim().TrimEnd('/') + "/";
        var blobName = basePath + originalName;

        var contentType = !string.IsNullOrWhiteSpace(file.ContentType)
            ? file.ContentType
            : GuessContentType(ext);

        var blobClient = container.GetBlobClient(blobName);
        if (await blobClient.ExistsAsync(cancellationToken))
        {
            var shortId = Guid.NewGuid().ToString("N")[..8];
            var nameNoExt = Path.GetFileNameWithoutExtension(originalName);
            blobName = $"{basePath}{nameNoExt}-{shortId}{ext}";
            blobClient = container.GetBlobClient(blobName);
        }

        await using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(
            stream,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
                Conditions = new BlobRequestConditions { IfNoneMatch = new ETag("*") }
            },
            cancellationToken);

        return blobClient.Uri;
    }

    private static string GuessContentType(string ext) => ext switch
    {
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        ".pdf" => "application/pdf",
        _ => "application/octet-stream"
    };
}
