// Copyright (c) Microsoft. All rights reserved.

internal static partial class Program
{
    private static readonly Argument<string> s_files = new("files", "Files to be processed");
    private static readonly Option<string> s_category = new("--category", "Value for the category field in the search index for all sections indexed in this run");
    private static readonly Option<bool> s_skipBlobs = new ("--skipblobs", "Skip uploading individual pages to Azure Blob Storage");
    private static readonly Option<string> s_storageAccount = new("--storageaccount", "Azure Blob Storage account name");
    private static readonly Option<string> s_container = new("--container", "Azure Blob Storage container name");
    private static readonly Option<string> s_storageKey = new("--storagekey", "Optional. Use this Azure Blob Storage account key instead of the current user identity to login (use az login to set current user for Azure)");
    private static readonly Option<string> s_tenantId = new("--tenantid", "Optional. Use this to define the Azure directory where to authenticate)");
    private static readonly Option<string> s_searchService = new("--searchservice", "Name of the Azure Cognitive Search service where content should be indexed (must exist already)");
    private static readonly Option<string> s_index = new("--index", "Name of the Azure Cognitive Search index where content should be indexed (will be created if it doesn't exist)");
    private static readonly Option<string> s_searchKey = new("--searchkey", "Optional. Use this Azure Cognitive Search account key instead of the current user identity to login (use az login to set current user for Azure)");
    private static readonly Option<bool> s_remove = new("--remove", "Remove references to this document from blob storage and the search index");
    private static readonly Option<bool> s_removeAll = new("--removeall", "Remove all blobs from blob storage and documents from the search index");
    private static readonly Option<bool> s_localPdfParser = new("--localpdfparser", "Use PyPdf local PDF parser (supports only digital PDFs) instead of Azure Form Recognizer service to extract text, tables and layout from the documents");
    private static readonly Option<string> s_formRecognizerService = new ("--formrecognizerservice", "Optional. Name of the Azure Form Recognizer service which will be used to extract text, tables and layout from the documents (must exist already)");
    private static readonly Option<string> s_formRecognizerKey = new("--formrecognizerkey", "Optional. Use this Azure Form Recognizer account key instead of the current user identity to login (use az login to set current user for Azure)");
    private static readonly Option<bool> s_verbose = new (new[] { "--verbose", "-v" }, "Verbose output");

    private static readonly RootCommand s_rootCommand = new(
       description: """
        Prepare documents by extracting content from PDFs, splitting content into sections,
        uploading to blob storage, and indexing in a search index.
        """)
    {
        s_files, s_category, s_skipBlobs, s_storageAccount, s_container, s_storageKey,
        s_tenantId, s_searchService, s_index, s_searchKey, s_remove, s_removeAll,
        s_localPdfParser, s_formRecognizerService, s_formRecognizerKey, s_verbose
    };
}
