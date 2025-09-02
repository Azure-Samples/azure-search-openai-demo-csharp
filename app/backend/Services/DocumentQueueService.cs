using System.Collections.Concurrent;
using System.Configuration;
using Azure.Messaging.ServiceBus;
using SharedWebApp.Models;

namespace MinimalApi.Services;

public interface IDocumentQueueService
{
    Task<bool> QueueDocumentAsync(DocumentQueueMessage message);
    Task<ProcessingStatus?> GetStatusAsync(string documentId);
    Task<List<ProcessingStatus>> GetAllStatusesAsync();
    void Dispose();
}

public sealed class DocumentQueueService : IDocumentQueueService, IDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;
    private readonly ILogger<DocumentQueueService> _logger;
    private readonly string _queueName = "document-processing";

    private static readonly ConcurrentDictionary<string, ProcessingStatus> s_statuses = new();

    public DocumentQueueService(IConfiguration configuration, ILogger<DocumentQueueService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var serviceBusNamespace = configuration["AZURE_SERVICE_BUS_NAMESPACE"]
            ?? throw new ConfigurationErrorsException("AZURE_SERVICE_BUS_NAMESPACE");

        _client = new ServiceBusClient($"{serviceBusNamespace}.servicebus.windows.net", new DefaultAzureCredential());
        _sender = _client.CreateSender(_queueName);
    }

    public async Task<bool> QueueDocumentAsync(DocumentQueueMessage message)
    {
        try
        {
            s_statuses.TryAdd(message.DocumentId, new ProcessingStatus
            {
                DocumentId = message.DocumentId,
                Status = "Queued",
                QueuedAt = message.QueuedAt
            });

            var messageBody = JsonSerializer.Serialize(message);
            var serviceBusMessage = new ServiceBusMessage(messageBody)
            {
                MessageId = message.DocumentId,
                ContentType = "application/json"
            };

            await _sender.SendMessageAsync(serviceBusMessage);

            _logger.LogInformation("Queued document {documentID} for processing", message.DocumentId);
            return true;
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Failed to queue document {documentId}", message.DocumentId);

            if (s_statuses.TryGetValue(message.DocumentId, out var status))
            {
                status.Status = "Failed";
                status.ErrorMessage = ex.Message;
                status.CompletedAt = DateTime.UtcNow;
            }

            return false;
        }


    }
    public Task<ProcessingStatus?> GetStatusAsync(string documentId)
    {
        s_statuses.TryGetValue(documentId, out var status);
        return Task.FromResult(status);
    }

    public Task<List<ProcessingStatus>> GetAllStatusesAsync()
    {
        return Task.FromResult(s_statuses.Values.OrderByDescending(s => s.QueuedAt).ToList());
    }

    public static void UpdateStatus(string documentId, string status, string? errorMessage = null)
    {
        if (s_statuses.TryGetValue(documentId, out var existing))
        {
            existing.Status = status;
            existing.ErrorMessage = errorMessage;

            if (status == "Processing")
            {
                existing.StartedAt = DateTime.UtcNow;
            }
            else if (status == "Processing")
            {
                existing.CompletedAt = DateTime.UtcNow;
            }
                
        }
    }
    public void Dispose()
    {
        _sender?.DisposeAsync().AsTask().Wait();
        _client?.DisposeAsync().AsTask().Wait();
    }
}
