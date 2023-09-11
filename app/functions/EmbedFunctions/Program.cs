// Copyright (c) Microsoft. All rights reserved.

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
                GetUriFromEnvironment("AZURE_STORAGE_ACCOUNT_ENDPOINT"),
                credential);

            return blobServiceClient.GetBlobContainerClient("corpus");
        });

        services.AddSingleton<EmbedServiceFactory>();
        services.AddSingleton<EmbeddingAggregateService>();

        services.AddSingleton<IEmbedService, AzureSearchEmbedService>();
        services.AddSingleton<IEmbedService, PineconeEmbedService>();
        services.AddSingleton<IEmbedService, QdrantEmbedService>();
        services.AddSingleton<IEmbedService, MilvusEmbedService>();
    })
    .ConfigureFunctionsWorkerDefaults()
    .Build();

host.Run();
