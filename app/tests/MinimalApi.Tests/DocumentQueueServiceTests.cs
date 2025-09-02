using Azure.Messaging.ServiceBus;
using Bogus;
using MinimalApi.Services;
using NSubstitute;
using Shouldly;
using SharedWebApp.Models;
using Xunit;
using Microsoft.Extensions.Logging;

namespace MinimalApi.Tests;

public class DocumentQueueServiceTests
{
    [Fact]
    public async Task QueueDocumentAsync_ShouldSendMessageAndTrackStatus()
    {
        var sender = Substitute.For<IServiceBusSender>();
        var logger = Substitute.For<ILogger<DocumentQueueService>>();
        var service = new DocumentQueueService(sender, logger);

        var message = new Faker<DocumentQueueMessage>()
            .RuleFor(m => m.DocumentId, f => f.Random.Guid().ToString("N").Substring(0, 12))
            .RuleFor(m => m.BlobUri, f => f.Internet.Url())
            .RuleFor(m => m.FileName, f => f.System.FileName())
            .Generate();

        var result = await service.QueueDocumentAsync(message);
        var status = await service.GetStatusAsync(message.DocumentId);

        result.ShouldBeTrue();
        await sender.Received(1).SendMessageAsync(Arg.Any<ServiceBusMessage>());
        status.ShouldNotBeNull();
        status!.Status.ShouldBe("Queued");
    }
}
