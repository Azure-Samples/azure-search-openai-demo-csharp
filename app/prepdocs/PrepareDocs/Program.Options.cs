// Copyright (c) Microsoft. All rights reserved.

internal static partial class Program
{
    private static readonly Argument<string> s_files =
        new(name: "files", description: "Files to be processed");

    private static readonly Option<string> s_category =
        new(name: "--category", description: "Value for the category field in the search index for all sections indexed in this run");

    private static readonly Option<bool> s_skipBlobs =
        new(name: "--skipblobs", description: "Skip uploading individual pages to Azure Blob Storage");

    private static readonly Option<string> s_storageEndpoint =
        new(name: "--storageendpoint", description: "Azure Blob Storage service endpoint");

    private static readonly Option<string> s_container =
        new(name: "--container", description: "Azure Blob Storage container name");

    private static readonly Option<string> s_tenantId =
        new(name: "--tenantid", description: "Optional. Use this to define the Azure directory where to authenticate)");

    private static readonly Option<string> s_searchService =
        new(name: "--searchendpoint", description: "The Azure AI Search service endpoint where content should be indexed (must exist already)");

    private static readonly Option<string> s_searchIndexName =
        new(name: "--searchindex", description: "Name of the Azure AI Search index where content should be indexed (will be created if it doesn't exist)");

    private static readonly Option<string> s_azureOpenAIService =
        new(name: "--openaiendpoint", description: "Optional. The Azure OpenAI service endpoint which will be used to extract text, tables and layout from the documents (must exist already)");

    private static readonly Option<string> s_embeddingModelName =
        new(name: "--embeddingmodel", description: "Optional. Name of the Azure AI Search embedding model to use for embedding content in the search index (will be created if it doesn't exist)");

    private static readonly Option<bool> s_remove =
       new(name: "--remove", description: "Remove references to this document from blob storage and the search index");

    private static readonly Option<bool> s_removeAll =
        new(name: "--removeall", description: "Remove all blobs from blob storage and documents from the search index");

    private static readonly Option<string> s_formRecognizerServiceEndpoint =
        new(name: "--formrecognizerendpoint", description: "Optional. The Azure AI Document Intelligence service endpoint which will be used to extract text, tables and layout from the documents (must exist already)");

    private static readonly Option<string> s_computerVisionServiceEndpoint =
        new(name: "--computervisionendpoint", description: "Optional. The Azure Computer Vision service endpoint which will be used to vectorize image and query");

    private static readonly Option<bool> s_verbose =
       new(aliases: new[] { "--verbose", "-v" }, description: "Verbose output");

    private static readonly RootCommand s_rootCommand =
        new(description: """
        Prepare documents by extracting content from PDFs, splitting content into sections,
        uploading to blob storage, and indexing in a search index.
        """)
        {
            s_files,
            s_category,
            s_skipBlobs,
            s_storageEndpoint,
            s_container,
            s_tenantId,
            s_searchService,
            s_searchIndexName,
            s_azureOpenAIService,
            s_embeddingModelName,
            s_remove,
            s_removeAll,
            s_formRecognizerServiceEndpoint,
            s_computerVisionServiceEndpoint,
            s_verbose,
        };

    private static AppOptions GetParsedAppOptions(InvocationContext context) => new(
            Files: context.ParseResult.GetValueForArgument(s_files),
            Category: context.ParseResult.GetValueForOption(s_category),
            SkipBlobs: context.ParseResult.GetValueForOption(s_skipBlobs),
            StorageServiceBlobEndpoint: context.ParseResult.GetValueForOption(s_storageEndpoint),
            Container: context.ParseResult.GetValueForOption(s_container),
            TenantId: context.ParseResult.GetValueForOption(s_tenantId),
            SearchServiceEndpoint: context.ParseResult.GetValueForOption(s_searchService),
            SearchIndexName: context.ParseResult.GetValueForOption(s_searchIndexName),
            AzureOpenAIServiceEndpoint: context.ParseResult.GetValueForOption(s_azureOpenAIService),
            EmbeddingModelName: context.ParseResult.GetValueForOption(s_embeddingModelName),
            Remove: context.ParseResult.GetValueForOption(s_remove),
            RemoveAll: context.ParseResult.GetValueForOption(s_removeAll),
            FormRecognizerServiceEndpoint: context.ParseResult.GetValueForOption(s_formRecognizerServiceEndpoint),
            ComputerVisionServiceEndpoint: context.ParseResult.GetValueForOption(s_computerVisionServiceEndpoint),
            Verbose: context.ParseResult.GetValueForOption(s_verbose),
            Console: context.Console);
}
