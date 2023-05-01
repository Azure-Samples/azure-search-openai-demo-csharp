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

        services.AddSingleton<DocumentAnalysisClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var azureFormRecognizerService = config["AZURE_FORM_RECOGNIZER_SERVICE"];
            var documentAnalysisClient = new DocumentAnalysisClient(
                new Uri($"https://{azureFormRecognizerService}.cognitiveservices.azure.com"), s_azureCredential);
            return documentAnalysisClient;
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

    internal static IServiceCollection AddMemoryStore(this IServiceCollection services)
    {
        services.AddSingleton<IMemoryStore>((sp) =>
        {
            var logger = sp.GetRequiredService<ILogger<IMemoryStore>>();
            logger.LogInformation("Loading corpus ...");
            var blobServiceClient = sp.GetRequiredService<BlobServiceClient>();
            var blobContainerClient = blobServiceClient.GetBlobContainerClient("corpus");
            var blobs = blobContainerClient.GetBlobs();
            var corpus = new List<CorpusRecord>();
            foreach (var blob in blobs)
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(blob.Name);
                var source = $"{fileNameWithoutExtension}.pdf";
                var readStream = blobContainerClient.GetBlobClient(blob.Name).OpenRead();
                var content = new StreamReader(readStream).ReadToEnd();

                // split contents into short sentences
                var sentences = content.Split(new[] { '.', '?', '!' }, StringSplitOptions.RemoveEmptyEntries);
                var corpusIndex = 0;
                var sb = new StringBuilder();
                // create corpus records based on sentences
                foreach (var sentence in sentences)
                {
                    sb.Append(sentence);
                    if (sb.Length > 256)
                    {
                        var id = $"{source}+{corpusIndex++}";
                        corpus.Add(new CorpusRecord(id, source, sb.ToString()));
                        sb.Clear();
                    }
                }
            }

            logger.LogInformation($"Load {corpus.Count} records into corpus");
            logger.LogInformation("Loading corpus into memory...");
            var embeddingService = new SentenceEmbeddingService(corpus);
            var collectionName = "knowledge";
            var memoryStore = new VolatileMemoryStore();
            memoryStore.CreateCollectionAsync(collectionName).Wait();
            var embeddings = embeddingService.GenerateEmbeddingsAsync(corpus.Select(c => c.Text).ToList()).Result;
            var memoryRecords = Enumerable.Zip(corpus, embeddings)
                                    .Select((tuple) =>
                                    {
                                        var (corpusRecord, embedding) = tuple;
                                        var metaData = new MemoryRecordMetadata(true, corpusRecord.Id, corpusRecord.Text, corpusRecord.Source, string.Empty, string.Empty);
                                        var memoryRecord = new MemoryRecord(metaData, embedding, key: corpusRecord.Id);
                                        return memoryRecord;
                                    });

            var _ = memoryStore.UpsertBatchAsync(collectionName, memoryRecords).ToListAsync().Result;

            return memoryStore;
        });

        return services;
    }
}
