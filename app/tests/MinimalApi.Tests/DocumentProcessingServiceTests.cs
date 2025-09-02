using Bogus;
using MinimalApi.Services;
using NSubstitute;
using Shouldly;
using SharedWebApp.Models;
using Xunit;

namespace MinimalApi.Tests;

public class DocumentProcessingServiceTests
{
    [Fact]
    public async Task ProcessDocumentAsync_ShouldEmbedDocument()
    {
        var embedService = Substitute.For<IEmbedService>();
        var blobService = Substitute.For<IAzureBlobStorageService>();
        var service = new DocumentProcessingService(embedService, blobService);

        var message = new Faker<DocumentQueueMessage>()
            .RuleFor(m => m.DocumentId, f => f.Random.Guid().ToString("N"))
            .RuleFor(m => m.BlobUri, f => f.Internet.Url())
            .RuleFor(m => m.FileName, "file.pdf")
            .Generate();

        blobService.OpenReadAsync(Arg.Any<Uri>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Stream>(new MemoryStream()));
        embedService.EmbedPDFBlobAsync(Arg.Any<Stream>(), message.FileName)
            .Returns(true);

        var result = await service.ProcessDocumentAsync(message);

        result.ShouldBeTrue();
        await embedService.Received(1).EmbedPDFBlobAsync(Arg.Any<Stream>(), message.FileName);
    }
}
