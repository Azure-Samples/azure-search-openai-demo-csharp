using System.Text.Json.Serialization;

namespace SharedWebApp.Models;

public class DocumentQueueMessage
{
    [JsonPropertyName("documentId")]
    public string DocumentId { get; set; } = string.Empty;

    [JsonPropertyName("blobUri")]
    public string BlobUri { get; set; } = string.Empty;

    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("queuedAt")]
    public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
}