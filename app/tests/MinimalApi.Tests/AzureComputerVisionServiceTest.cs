// Copyright (c) Microsoft. All rights reserved.

using FluentAssertions;
using MinimalApi.Services;
using NSubstitute;

namespace MinimalApi.Tests;

public class AzureComputerVisionServiceTest
{
    [ApiKeyFact("AZURE_COMPUTER_VISION_API_KEY", "AZURE_COMPUTER_VISION_ENDPOINT")]
    public async Task VectorizeImageTestAsync()
    {
        var endpoint = Environment.GetEnvironmentVariable("AZURE_COMPUTER_VISION_ENDPOINT") ?? throw new InvalidOperationException();
        var apiKey = Environment.GetEnvironmentVariable("AZURE_COMPUTER_VISION_API_KEY") ?? throw new InvalidOperationException();
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient().ReturnsForAnyArgs(x => new HttpClient());
        var service = new AzureComputerVisionService(httpClientFactory, endpoint, apiKey);
        var imageUrl = @"https://learn.microsoft.com/azure/ai-services/computer-vision/media/quickstarts/presentation.png";

        var result = await service.VectorizeImageAsync(imageUrl);

        result.modelVersion.Should().NotBeNullOrEmpty();

        // download image to local file, and verify the api on local image.
        var tempFile = Path.GetTempFileName();
        tempFile = Path.ChangeExtension(tempFile, ".png");
        try
        {
            using var client = new HttpClient();
            using var stream = await client.GetStreamAsync(imageUrl);
            using var fileStream = File.OpenWrite(tempFile);
            await stream.CopyToAsync(fileStream);
            fileStream.Flush();
            fileStream.Close();

            var localResult = await service.VectorizeImageAsync(tempFile);

            localResult.modelVersion.Should().NotBeNullOrEmpty();
            localResult.vector.Should().BeEquivalentTo(result.vector);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [ApiKeyFact("AZURE_COMPUTER_VISION_API_KEY", "AZURE_COMPUTER_VISION_ENDPOINT")]
    public async Task VectorizeTextTestAsync()
    {
        var endpoint = Environment.GetEnvironmentVariable("AZURE_COMPUTER_VISION_ENDPOINT") ?? throw new InvalidOperationException();
        var apiKey = Environment.GetEnvironmentVariable("AZURE_COMPUTER_VISION_API_KEY") ?? throw new InvalidOperationException();
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient().ReturnsForAnyArgs(x => new HttpClient());
        var service = new AzureComputerVisionService(httpClientFactory, endpoint, apiKey);
        var text = "Hello world";
        var result = await service.VectorizeTextAsync(text);

        result.modelVersion.Should().NotBeNullOrEmpty();
        result.vector.Length.Should().Be(1024);
    }
}
