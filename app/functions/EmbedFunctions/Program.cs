// Copyright (c) Microsoft. All rights reserved.

using Azure.AI.OpenAI;

var host = new HostBuilder()
    .ConfigureServices(services =>
    {
        var credential = new DefaultAzureCredential();

        static Uri GetUriFromEnvironment(string variable) => Environment.GetEnvironmentVariable(variable) is string value &&
                Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) &&
                uri is not null
                ? uri
                : throw new ArgumentException(
                $"Unable to parse URI from environment variable: {variable}");

        services.AddAzureClients(builder =>
        {
            builder.AddDocumentAnalysisClient(
                GetUriFromEnvironment("AZURE_FORMRECOGNIZER_SERVICE_ENDPOINT"));
        });

        services.AddSingleton<SearchClient>(_ =>
        {
            return new SearchClient(
                GetUriFromEnvironment("AZURE_SEARCH_SERVICE_ENDPOINT"),
                Environment.GetEnvironmentVariable("AZURE_SEARCH_INDEX"),
                credential);
        });

        services.AddSingleton<SearchIndexClient>(_ =>
        {
            return new SearchIndexClient(
                GetUriFromEnvironment("AZURE_SEARCH_SERVICE_ENDPOINT"),
                credential);
        });

        services.AddSingleton<BlobContainerClient>(_ =>
        {
            var blobServiceClient = new BlobServiceClient(
                GetUriFromEnvironment("AZURE_STORAGE_BLOB_ENDPOINT"),
                credential);

            var containerClient = blobServiceClient.GetBlobContainerClient("corpus");

            containerClient.CreateIfNotExists();

            return containerClient;
        });

        services.AddSingleton<EmbedServiceFactory>();
        services.AddSingleton<EmbeddingAggregateService>();

        services.AddSingleton<IEmbedService, AzureSearchEmbedService>(provider =>
        {
            var searchIndexName = Environment.GetEnvironmentVariable("AZURE_SEARCH_INDEX") ?? throw new ArgumentNullException("AZURE_SEARCH_INDEX is null");
            var embeddingModelName = Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_DEPLOYMENT") ?? throw new ArgumentNullException("AZURE_OPENAI_EMBEDDING_DEPLOYMENT is null");
            var openaiEndPoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? throw new ArgumentNullException("AZURE_OPENAI_ENDPOINT is null");

            var openAIClient = new OpenAIClient(new Uri(openaiEndPoint), new DefaultAzureCredential());

            var searchClient = provider.GetRequiredService<SearchClient>();
            var searchIndexClient = provider.GetRequiredService<SearchIndexClient>();
            var blobContainerClient = provider.GetRequiredService<BlobContainerClient>();
            var documentClient = provider.GetRequiredService<DocumentAnalysisClient>();
            var logger = provider.GetRequiredService<ILogger<AzureSearchEmbedService>>();

            return new AzureSearchEmbedService(openAIClient, embeddingModelName, searchClient, searchIndexName, searchIndexClient, documentClient, blobContainerClient, logger);
        });
    })
    .ConfigureFunctionsWorkerDefaults()
    .Build();

host.Run();
