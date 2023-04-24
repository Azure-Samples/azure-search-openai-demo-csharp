// Copyright (c) Microsoft. All rights reserved.

using Azure;
using Azure.Identity;

var files = new Argument<string>("files", "Files to be processed");
var category = new Option<string>("--category", "Value for the category field in the search index for all sections indexed in this run");
var skipblobs = new Option<bool>("--skipblobs", "Skip uploading individual pages to Azure Blob Storage");
var storageaccount = new Option<string>("--storageaccount", "Azure Blob Storage account name");
var container = new Option<string>("--container", "Azure Blob Storage container name");
var storagekey = new Option<string>("--storagekey", "Optional. Use this Azure Blob Storage account key instead of the current user identity to login (use az login to set current user for Azure)");
var tenantid = new Option<string>("--tenantid", "Optional. Use this to define the Azure directory where to authenticate)");
var searchservice = new Option<string>("--searchservice", "Name of the Azure Cognitive Search service where content should be indexed (must exist already)");
var index = new Option<string>("--index", "Name of the Azure Cognitive Search index where content should be indexed (will be created if it doesn't exist)");
var searchkey = new Option<string>("--searchkey", "Optional. Use this Azure Cognitive Search account key instead of the current user identity to login (use az login to set current user for Azure)");
var remove = new Option<bool>("--remove", "Remove references to this document from blob storage and the search index");
var removeall = new Option<bool>("--removeall", "Remove all blobs from blob storage and documents from the search index");
var localpdfparser = new Option<bool>("--localpdfparser", "Use PyPdf local PDF parser (supports only digital PDFs) instead of Azure Form Recognizer service to extract text, tables and layout from the documents");
var formrecognizerservice = new Option<string>("--formrecognizerservice", "Optional. Name of the Azure Form Recognizer service which will be used to extract text, tables and layout from the documents (must exist already)");
var formrecognizerkey = new Option<string>("--formrecognizerkey", "Optional. Use this Azure Form Recognizer account key instead of the current user identity to login (use az login to set current user for Azure)");
var verbose = new Option<bool>(new[] { "--verbose", "-v" }, "Verbose output");
var rootCommand = new RootCommand(
    """
    Prepare documents by extracting content from PDFs, splitting content into sections,
    uploading to blob storage, and indexing in a search index.
    """)
{
    files, category, skipblobs, storageaccount, container, storagekey,
    tenantid, searchservice, index, searchkey, remove, removeall,
    localpdfparser, formrecognizerservice, formrecognizerkey, verbose
};

rootCommand.SetHandler(
    async (context) =>
    {
        var credential = context.ParseResult.GetValueForOption(tenantid) is string tenantId
            ? new AzureCliCredential(new AzureCliCredentialOptions { TenantId = tenantId })
            : new AzureCliCredential();

        var searchCredential = context.ParseResult.GetValueForOption(searchkey) is string searchKey
            ? new AzureKeyCredential(searchKey)
            : null;



        await Task.CompletedTask;
    });

return await rootCommand.InvokeAsync(args);
