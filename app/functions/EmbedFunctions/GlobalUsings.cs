// Copyright (c) Microsoft. All rights reserved.

global using System.Net;
global using System.Text;
global using System.Text.RegularExpressions;
global using Azure;
global using Azure.AI.FormRecognizer.DocumentAnalysis;
global using Azure.Identity;
global using Azure.Search.Documents;
global using Azure.Search.Documents.Indexes;
global using Azure.Search.Documents.Indexes.Models;
global using Azure.Search.Documents.Models;
global using Azure.Storage.Blobs;
global using Azure.Storage.Blobs.Models;
global using EmbedFunctions.Services;
global using Microsoft.Extensions.Azure;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;
global using Shared;
global using Shared.Models;
