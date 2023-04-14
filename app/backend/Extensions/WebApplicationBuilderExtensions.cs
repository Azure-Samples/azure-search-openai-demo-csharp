// Copyright (c) Microsoft. All rights reserved.

namespace Backend.Extensions;

internal static class WebApplicationBuilderExtensions
{
    internal static WebApplicationBuilder AddAzureServices(this WebApplicationBuilder builder)
    {
        // TODO: Shouldn't we be using the Azure SDK for .NET's built-in DI APIs?

        var azureCredential = new DefaultAzureCredential();
        var azureStorageAccount = builder.Configuration["AZURE_STORAGE_ACCOUNT"];

        // Add blob service client
        var blobServiceClient = new BlobServiceClient(
            new Uri($"https://{azureStorageAccount}.blob.core.windows.net"), azureCredential);
        builder.Services.AddSingleton(blobServiceClient);

        // Add blob container client
        var azureStorageContainer = builder.Configuration["AZURE_STORAGE_CONTAINER"];
        var blobContainerClient = blobServiceClient.GetBlobContainerClient(azureStorageContainer);
        builder.Services.AddSingleton(blobContainerClient);

        // Add search client
        var azureSearchService = builder.Configuration["AZURE_SEARCH_SERVICE"];
        var azureSearchIndex = builder.Configuration["AZURE_SEARCH_INDEX"];
        var searchClient = new SearchClient(
            new Uri($"https://{azureSearchService}.search.windows.net"),
            azureSearchIndex, azureCredential);
        builder.Services.AddSingleton(searchClient);

        // Add semantic kernel
        var azureOpenaiChatGPTDeployment = builder.Configuration["AZURE_OPENAI_CHATGPT_DEPLOYMENT"];
        ArgumentNullException.ThrowIfNullOrEmpty(azureOpenaiChatGPTDeployment);

        var azureOpenaiGPTDeployment = builder.Configuration["AZURE_OPENAI_GPT_DEPLOYMENT"];
        var azureOpenaiService = builder.Configuration["AZURE_OPENAI_SERVICE"];
        var openAIClient = new OpenAIClient(
            new Uri($"https://{azureOpenaiService}.openai.azure.com"), azureCredential);

        // Semantic Kernel doesn't support Azure AAD credential for now
        // so we implement our own text completion backend
        var openAIService = new AzureOpenAITextCompletionService(
            openAIClient, azureOpenaiGPTDeployment!);
        var kernel = Kernel.Builder.Build();
        kernel.Config.AddTextCompletionService(azureOpenaiGPTDeployment!, _ => openAIService, true);
        builder.Services.AddSingleton(kernel);
        builder.Services.AddSingleton(openAIService);
        builder.Services.AddSingleton(
            new RetrieveThenReadApproachService(searchClient, kernel));
        builder.Services.AddSingleton(new ReadRetrieveReadChatService(searchClient, kernel));
        builder.Services.AddSingleton(new ReadRetrieveReadApproachService(searchClient, openAIService));
        builder.Services.AddSingleton<ReadDecomposeAskApproachService>();

        return builder;
    }
}
