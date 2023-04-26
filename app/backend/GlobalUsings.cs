// Copyright (c) Microsoft. All rights reserved.

global using System.Runtime.CompilerServices;
global using System.Text;
global using Azure.AI.OpenAI;
global using Azure.Identity;
global using Azure.Search.Documents;
global using Azure.Search.Documents.Models;
global using Azure.Storage.Blobs;
global using Microsoft.Extensions.Caching.Memory;
global using Microsoft.Net.Http.Headers;
global using Microsoft.SemanticKernel;
global using Microsoft.SemanticKernel.AI;
global using Microsoft.SemanticKernel.AI.TextCompletion;
global using Microsoft.SemanticKernel.CoreSkills;
global using Microsoft.SemanticKernel.Orchestration;
global using Microsoft.SemanticKernel.SkillDefinition;
global using MinimalApi.Extensions;
global using MinimalApi.Services;
global using MinimalApi.Services.Skills;
global using Shared.Models;
