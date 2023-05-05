// Copyright (c) Microsoft. All rights reserved.

global using System.CommandLine;
global using System.CommandLine.Invocation;
global using System.CommandLine.Parsing;
global using System.Linq;
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
global using Microsoft.Extensions.FileSystemGlobbing;
global using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
global using PdfSharpCore.Pdf;
global using PdfSharpCore.Pdf.IO;
global using PrepareDocs;
