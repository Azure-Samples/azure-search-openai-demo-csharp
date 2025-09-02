using Azure.Messaging.ServiceBus;
using SharedWebApp.Models;

namespace MinimalApi.Services;

public sealed class DocumentProcessor : BackgroundService, IAsyncDisposable
{
    private readonly ServiceBusProcessor _processor;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DocumentProcessor> _logger;

    public DocumentProcessor(
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        ILogger<DocumentProcessor> logger,
        ServiceBusClient client)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var queueName = configuration["DOCUMENT_QUEUE_NAME"] ?? "document-processing";
        var options = new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = 2,
            AutoCompleteMessages = false
        };

        _processor = client.CreateProcessor(queueName, options);
        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting document processor");

        await _processor.StartProcessingAsync(stoppingToken);
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Document processor is stopping");
        }
        finally
        {
            await _processor.StopProcessingAsync();
        }
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        try
        {
            var messageBody = args.Message.Body.ToString();
            var queueMessage = JsonSerializer.Deserialize<DocumentQueueMessage>(messageBody);

            if (queueMessage == null)
            {
                _logger.LogError("Invalid message format for message ID {MessageId}", args.Message.MessageId);
                await args.DeadLetterMessageAsync(args.Message, "Invalid format", "Could not deserialize message");
                return;
            }

            _logger.LogInformation("Processing document {DocumentId} from queue", queueMessage.DocumentId);
            DocumentQueueService.UpdateStatus(queueMessage.DocumentId, "Processing");

            using var scope = _scopeFactory.CreateScope();
            var processingService = scope.ServiceProvider.GetRequiredService<IDocumentProcessingService>();
            var success = await processingService.ProcessDocumentAsync(queueMessage);

            if (success)
            {
                DocumentQueueService.UpdateStatus(queueMessage.DocumentId, "Completed");
                await args.CompleteMessageAsync(args.Message);
                _logger.LogInformation("Successfully processed document {DocumentId}", queueMessage.DocumentId);
            }
            else
            {
                DocumentQueueService.UpdateStatus(queueMessage.DocumentId, "Failed", "Processing failed");

                if (args.Message.DeliveryCount < 3)
                {
                    await args.AbandonMessageAsync(args.Message);
                    _logger.LogWarning("Processing failed for {DocumentId}, will retry (attempt {Count})",
                        queueMessage.DocumentId, args.Message.DeliveryCount);
                }
                else
                {
                    await args.DeadLetterMessageAsync(args.Message, "Max retries exceeded", "Failed after 3 attempts");
                    _logger.LogError("Max retries exceeded for document {DocumentId}", queueMessage.DocumentId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message {MessageId}", args.Message.MessageId);

            if (args.Message.DeliveryCount < 3)
            {
                await args.AbandonMessageAsync(args.Message);
            }
            else
            {
                await args.DeadLetterMessageAsync(args.Message, "Processing error", ex.Message);
            }
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Service Bus processing error from {Source}", args.ErrorSource);
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        return ((IAsyncDisposable)_processor).DisposeAsync();
    }
}
