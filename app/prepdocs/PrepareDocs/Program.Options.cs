// Copyright (c) Microsoft. All rights reserved.

internal static partial class Program
{
    private static readonly Argument<string> s_files =
        new(name: "files", description: "Files to be processed");

    private static readonly Option<string> s_category =
        new(name: "--category", description: "Value for the category field in the search index for all sections indexed in this run");

    private static readonly Option<bool> s_skipBlobs =
        new(name: "--skipblobs", description: "Skip uploading individual pages to Azure Blob Storage");

    private static readonly Option<string> s_storageAccount =
        new(name: "--storageaccount", description: "Azure Blob Storage account name");

    private static readonly Option<string> s_container =
        new(name: "--container", description: "Azure Blob Storage container name");

    private static readonly Option<string> s_tenantId =
        new(name: "--tenantid", description: "Optional. Use this to define the Azure directory where to authenticate)");

    private static readonly Option<string> s_searchService =
        new(name: "--searchservice", description: "Name of the Azure Cognitive Search service where content should be indexed (must exist already)");

    private static readonly Option<string> s_index =
        new(name: "--index", description: "Name of the Azure Cognitive Search index where content should be indexed (will be created if it doesn't exist)");

     private static readonly Option<bool> s_remove =
        new(name: "--remove", description: "Remove references to this document from blob storage and the search index");

    private static readonly Option<bool> s_removeAll =
        new(name: "--removeall", description: "Remove all blobs from blob storage and documents from the search index");

    private static readonly Option<string> s_formRecognizerService =
        new(name: "--formrecognizerservice", description: "Optional. Name of the Azure Form Recognizer service which will be used to extract text, tables and layout from the documents (must exist already)");

     private static readonly Option<bool> s_verbose =
        new(aliases: new[] { "--verbose", "-v" }, description: "Verbose output");

    private static readonly RootCommand s_rootCommand =
        new(description: """
        Prepare documents by extracting content from PDFs, splitting content into sections,
        uploading to blob storage, and indexing in a search index.
        """)
    {
        s_files, s_category, s_skipBlobs, s_storageAccount,
        s_container, s_tenantId, s_searchService, s_index,
        s_remove, s_removeAll, s_formRecognizerService, s_verbose
    };

    private static AppOptions GetParsedAppOptions(InvocationContext context) =>
        new(
            Files: context.ParseResult.GetValueForArgument(s_files),
            Category: context.ParseResult.GetValueForOption(s_category),
            SkipBlobs: context.ParseResult.GetValueForOption(s_skipBlobs),
            StorageAccount: context.ParseResult.GetValueForOption(s_storageAccount),
            Container: context.ParseResult.GetValueForOption(s_container),
            TenantId: context.ParseResult.GetValueForOption(s_tenantId),
            SearchService: context.ParseResult.GetValueForOption(s_searchService),
            Index: context.ParseResult.GetValueForOption(s_index),
            Remove: context.ParseResult.GetValueForOption(s_remove),
            RemoveAll: context.ParseResult.GetValueForOption(s_removeAll),
            FormRecognizerService: context.ParseResult.GetValueForOption(s_formRecognizerService),
            Verbose: context.ParseResult.GetValueForOption(s_verbose),
            Console: context.Console);

}
