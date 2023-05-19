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
            var azureStorageAccountEndpoint = config["AzureStorageAccountEndpoint"];
            ArgumentNullException.ThrowIfNullOrEmpty(azureStorageAccountEndpoint);

            var blobServiceClient = new BlobServiceClient(
                new Uri(azureStorageAccountEndpoint), s_azureCredential);

            return blobServiceClient;
        });

        services.AddSingleton<BlobContainerClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var azureStorageContainer = config["AzureStorageContainer"];
            return sp.GetRequiredService<BlobServiceClient>().GetBlobContainerClient(azureStorageContainer);
        });

        services.AddSingleton<SearchClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var (azureSearchServiceEndpoint, azureSearchIndex) =
                (config["AzureSearchServiceEndpoint"], config["AzureSearchIndex"]);

            ArgumentNullException.ThrowIfNullOrEmpty(azureSearchServiceEndpoint);

            var searchClient = new SearchClient(
                new Uri(azureSearchServiceEndpoint), azureSearchIndex, s_azureCredential);

            return searchClient;
        });

        services.AddSingleton<DocumentAnalysisClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var azureOpenAiServiceEndpoint = config["AzureOpenAiServiceEndpoint"];
            ArgumentNullException.ThrowIfNullOrEmpty(azureOpenAiServiceEndpoint);

            var documentAnalysisClient = new DocumentAnalysisClient(
                new Uri(azureOpenAiServiceEndpoint), s_azureCredential);
            return documentAnalysisClient;
        });

        services.AddSingleton<OpenAIClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var azureOpenAiServiceEndpoint = config["AzureOpenAiServiceEndpoint"];

            ArgumentNullException.ThrowIfNullOrEmpty(azureOpenAiServiceEndpoint);

            var openAIClient = new OpenAIClient(
                new Uri(azureOpenAiServiceEndpoint), s_azureCredential);

            return openAIClient;
        });

        services.AddSingleton<IKernel>(sp =>
        {
            // Semantic Kernel doesn't support Azure AAD credential for now
            // so we implement our own text completion backend
            var config = sp.GetRequiredService<IConfiguration>();
            var azureOpenAiGptDeployment = config["AzureOpenAiGptDeployment"];

            var openAITextService = sp.GetRequiredService<AzureOpenAITextCompletionService>();
            var kernel = Kernel.Builder.Build();
            kernel.Config.AddTextCompletionService(azureOpenAiGptDeployment!, _ => openAITextService);

            return kernel;
        });

        services.AddSingleton<AzureOpenAITextCompletionService>();
        services.AddSingleton<AzureOpenAIChatCompletionService>();
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

    internal static IServiceCollection AddMemoryStore(this IServiceCollection services)
    {
        return services.AddSingleton<IMemoryStore, CorpusMemoryStore>();
    }
}
