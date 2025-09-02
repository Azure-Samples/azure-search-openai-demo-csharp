namespace Shared.Models;

public record class QueuedDocumentResult(
    string DocumentId,
    string FileName,
    string Status,
    Uri BlobUri);
