// Copyright (c) Microsoft. All rights reserved.

global using System.Diagnostics;
global using System.Runtime.CompilerServices;
global using System.Text;
global using System.Text.Json;
global using Azure.AI.FormRecognizer.DocumentAnalysis;
global using Azure.AI.OpenAI;
global using Azure.Identity;
global using Azure.Search.Documents;
global using Azure.Search.Documents.Models;
global using Azure.Storage.Blobs;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.AspNetCore.Mvc.RazorPages;
global using Microsoft.Extensions.Caching.Distributed;
global using Microsoft.ML;
global using Microsoft.ML.Transforms.Text;
global using Microsoft.SemanticKernel;
global using Microsoft.SemanticKernel.AI;
global using Microsoft.SemanticKernel.AI.Embeddings;
global using Microsoft.SemanticKernel.AI.TextCompletion;
global using Microsoft.SemanticKernel.Memory;
global using Microsoft.SemanticKernel.Orchestration;
global using Microsoft.SemanticKernel.SkillDefinition;
global using MinimalApi.Extensions;
global using MinimalApi.Services;
global using MinimalApi.Services.Skills;
global using Shared.Models;
