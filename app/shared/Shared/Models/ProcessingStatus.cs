public class ProcessingStatus
{
    public string DocumentId { get; set; } = string.Empty;
    public string Status { get; set; } = "Queued";
    public DateTime QueuedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}
