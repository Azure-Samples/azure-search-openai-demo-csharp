namespace MinimalApi.Options;

public sealed class ServiceBusWorkerOptions
{
    public string Namespace { get; set; } = "";
    public string QueueName { get; set; } = "document-processing";
    public int MaxConcurrentCalls { get; set; } = 2;
    public bool AutoCompleteMessages { get; set; } = false;
    public int MaxDeliveryAttempts { get; set; } = 3;
}
