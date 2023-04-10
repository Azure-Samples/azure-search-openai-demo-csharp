// Copyright (c) Microsoft. All rights reserved.

global using System.Text;
global using System.Text.Json.Serialization;
global using Azure.AI.OpenAI;
global using Azure.Identity;
global using Azure.Search.Documents;
global using Azure.Search.Documents.Models;
global using Azure.Storage.Blobs;
global using Backend.Extensions;
global using Backend.Models;
global using Backend.Services;
global using Microsoft.SemanticKernel;
global using Microsoft.SemanticKernel.AI;
global using Microsoft.SemanticKernel.AI.TextCompletion;
global using Microsoft.SemanticKernel.Orchestration;
