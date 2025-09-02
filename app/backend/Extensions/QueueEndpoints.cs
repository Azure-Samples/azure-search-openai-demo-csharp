using SharedWebApp.Models;

namespace MinimalApi.Extensions;

public static class QueueEndpoints
{
    public static void MapQueueEndPoints(this WebApplication app)
    {
        app.MapPost("/api/documents/upload-and-queue", async (
            IFormFileCollection files,
            IDocumentQueueService queueService,
            IAzureBlobStorageService blobService,
            ILogger<Program> logger) =>
        {
            if (!files.Any())
            {
                return Results.BadRequest("No Files provided, please try again.");
            }

            var results = new List<object>();

            foreach (var file in files)
            {
                try
                {
                    var documentId = Guid.NewGuid().ToString("N")[..12];

                    var blobUri = await blobService.UploadFileAsync(file, $"documents/{documentId}");

                    var queueMessage = new DocumentQueueMessage
                    {
                        DocumentId = documentId,
                        BlobUri = blobUri.ToString(),
                        FileName = file.FileName,
                        Category = "Uploaded"
                    };

                    var queued = await queueService.QueueDocumentAsync(queueMessage);

                    results.Add(new
                    {
                        DocumentId = documentId,
                        FileName = file.FileName,
                        Status = queued ? "Queued" : "Failed",
                        BlobUri = blobUri
                    });

                    logger.LogInformation("File {FileName} uploaded and queued as {DocumentId}",
                        file.FileName, documentId);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to process file {FileName}", file.FileName);
                    results.Add(new
                    {
                        FileName = file.FileName,
                        Status = "Failed",
                        Error = ex.Message
                    });
                }
            }
            return Results.Ok(results);
        }).WithName("UploadAndQueueDocuments")
        .DisableAntiforgery();

        app.MapGet("/api/documents/status/{documentId}", async (
            string documentId,
            IDocumentQueueService queueService) =>
        {
            var status = await queueService.GetStatusAsync(documentId);
            return status != null ? Results.Ok(status) : Results.NotFound();
        })
        .WithName("GetDocumentStatus");

        app.MapGet("/api/documents/status", async (IDocumentQueueService queueService) =>
        {
            var statuses = await queueService.GetAllStatusesAsync();
            return Results.Ok(statuses);
        })
        .WithName("GetAllDocumentStatuses");
    }
}
