// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Extensions;

internal static class WebApplicationBuilderExtensions
{
    private static readonly DefaultAzureCredential s_azureCredential = new();

    internal static WebApplicationBuilder AddAzureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<BlobServiceClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var azureStorageAccount = config["AZURE_STORAGE_ACCOUNT"];
            var blobServiceClient = new BlobServiceClient(
                new Uri($"https://{azureStorageAccount}.blob.core.windows.net"), s_azureCredential);

            return blobServiceClient;
        });

        builder.Services.AddSingleton<BlobContainerClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var azureStorageContainer = config["AZURE_STORAGE_CONTAINER"];
            return sp.GetRequiredService<BlobServiceClient>().GetBlobContainerClient(azureStorageContainer);
        });

        builder.Services.AddSingleton<SearchClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var (azureSearchService, azureSearchIndex) = (config["AZURE_SEARCH_SERVICE"], config["AZURE_SEARCH_INDEX"]);
            var searchClient = new SearchClient(
                new Uri($"https://{azureSearchService}.search.windows.net"), azureSearchIndex, s_azureCredential);

            return searchClient;
        });
       
        builder.Services.AddSingleton<OpenAIClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var azureOpenAiService = config["AZURE_OPENAI_SERVICE"];
            var openAIClient = new OpenAIClient(
                new Uri($"https://{azureOpenAiService}.openai.azure.com"), s_azureCredential);

            return openAIClient;
        });
        
        builder.Services.AddSingleton<IKernel>(sp =>
        {
            // Semantic Kernel doesn't support Azure AAD credential for now
            // so we implement our own text completion backend
            var config = sp.GetRequiredService<IConfiguration>();
            var azureOpenAiGptDeployment = config["AZURE_OPENAI_GPT_DEPLOYMENT"];
            var openAIService = sp.GetRequiredService<AzureOpenAITextCompletionService>();
            var kernel = Kernel.Builder.Build();
            kernel.Config.AddTextCompletionService(
                azureOpenAiGptDeployment!, _ => openAIService, true);

            return kernel;
        });

        builder.Services.AddSingleton<AzureOpenAITextCompletionService>();
        builder.Services.AddSingleton<ReadRetrieveReadChatService>();

        builder.Services.AddSingleton<IApproachBasedService, RetrieveThenReadApproachService>();
        builder.Services.AddSingleton<IApproachBasedService, ReadRetrieveReadApproachService>();
        builder.Services.AddSingleton<IApproachBasedService, ReadDecomposeAskApproachService>();

        builder.Services.AddSingleton<ApproachServiceResponseFactory>();

        return builder;
    }
    
    internal static WebApplicationBuilder AddCrossOriginResourceSharing(this WebApplicationBuilder builder)
    {
        builder.Services.AddCors(
            options =>
                options.AddDefaultPolicy(
                    policy =>
                        policy.AllowAnyOrigin()
                            .AllowAnyHeader()
                            .AllowAnyMethod()));

        return builder;
    }
}
