// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Extensions;

internal static class ServiceCollectionExtensions
{
    private static readonly DefaultAzureCredential s_azureCredential = new();

    internal static IServiceCollection AddAzureServices(this IServiceCollection services)
    {
        services.AddSingleton<BlobServiceClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var azureStorageAccount = config["AZURE_STORAGE_ACCOUNT"];
            var blobServiceClient = new BlobServiceClient(
                new Uri($"https://{azureStorageAccount}.blob.core.windows.net"), s_azureCredential);

            return blobServiceClient;
        });

        services.AddSingleton<BlobContainerClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var azureStorageContainer = config["AZURE_STORAGE_CONTAINER"];
            return sp.GetRequiredService<BlobServiceClient>().GetBlobContainerClient(azureStorageContainer);
        });

        services.AddSingleton<SearchClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var (azureSearchService, azureSearchIndex) =
                (config["AZURE_SEARCH_SERVICE"], config["AZURE_SEARCH_INDEX"]);
            var searchClient = new SearchClient(
                new Uri($"https://{azureSearchService}.search.windows.net"), azureSearchIndex, s_azureCredential);

            return searchClient;
        });

        services.AddSingleton<OpenAIClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var azureOpenAiService = config["AZURE_OPENAI_SERVICE"];
            var openAIClient = new OpenAIClient(
                new Uri($"https://{azureOpenAiService}.openai.azure.com"), s_azureCredential);

            return openAIClient;
        });

        services.AddSingleton<IKernel>(sp =>
        {
            // Semantic Kernel doesn't support Azure AAD credential for now
            // so we implement our own text completion backend
            var config = sp.GetRequiredService<IConfiguration>();
            var azureOpenAiGptDeployment = config["AZURE_OPENAI_GPT_DEPLOYMENT"];
            var openAIService = sp.GetRequiredService<AzureOpenAITextCompletionService>();
            var kernel = Kernel.Builder.Build();
            kernel.Config.AddTextCompletionService(azureOpenAiGptDeployment!, _ => openAIService);

            return kernel;
        });

        services.AddSingleton<AzureOpenAITextCompletionService>();
        services.AddSingleton<ReadRetrieveReadChatService>();

        services.AddSingleton<IApproachBasedService, RetrieveThenReadApproachService>();
        services.AddSingleton<IApproachBasedService, ReadRetrieveReadApproachService>();
        services.AddSingleton<IApproachBasedService, ReadDecomposeAskApproachService>();

        services.AddSingleton<ApproachServiceResponseFactory>();

        return services;
    }

    internal static IServiceCollection AddCrossOriginResourceSharing(this IServiceCollection services)
    {
        services.AddCors(
            options =>
                options.AddDefaultPolicy(
                    policy =>
                        policy.AllowAnyOrigin()
                            .AllowAnyHeader()
                            .AllowAnyMethod()));

        return services;
    }
}
