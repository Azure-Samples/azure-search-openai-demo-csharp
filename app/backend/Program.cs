// Copyright (c) Microsoft. All rights reserved.

using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Storage.Blobs;
using Backend.Services;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
// See: https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var azureCredential = new DefaultAzureCredential();
var azureStorageAccount = builder.Configuration["AZURE_STORAGE_ACCOUNT"];

// Add blob service client
var blobServiceClient = new BlobServiceClient(new Uri($"https://{azureStorageAccount}.blob.core.windows.net"), azureCredential);
builder.Services.AddSingleton(blobServiceClient);

// Add blob container client
var azureStorageContainer = builder.Configuration["AZURE_STORAGE_CONTAINER"];
var blobContainerClient = blobServiceClient.GetBlobContainerClient(azureStorageContainer);
builder.Services.AddSingleton(blobContainerClient);

// Add search client
var azureSearchService = builder.Configuration["AZURE_SEARCH_SERVICE"];
var azureSearchIndex = builder.Configuration["AZURE_SEARCH_INDEX"];
var searchClient = new SearchClient(new Uri($"https://{azureSearchService}.search.windows.net"), azureSearchIndex, azureCredential);
builder.Services.AddSingleton(searchClient);

// add semantic kernel
var azureOpenaiChatGPTDeployment = builder.Configuration["AZURE_OPENAI_CHATGPT_DEPLOYMENT"];
var azureOpenaiGPTDeployment = builder.Configuration["AZURE_OPENAI_GPT_DEPLOYMENT"];
var azureOpenaiService = builder.Configuration["AZURE_OPENAI_SERVICE"];
var openAIClient = new OpenAIClient(new Uri($"https://{azureOpenaiService}.openai.azure.com"), azureCredential);

// Semantic Kernel doesn't support Azure AAD credential for now
// so we implement our own text completion backend
var openAIService = new AzureOpenAITextCompletionService(openAIClient, azureOpenaiGPTDeployment);
var kernel = Kernel.Builder.Build();
kernel.Config.AddTextCompletionService(azureOpenaiGPTDeployment, _ => openAIService, true);
builder.Services.AddSingleton(kernel);

// add RetrieveThenReadApproachService
builder.Services.AddSingleton(new RetrieveThenReadApproachService(searchClient, kernel));
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.UseDefaultFiles();
app.UseStaticFiles();

app.Run();
