// Copyright (c) Microsoft. All rights reserved.

global using System.Globalization;
global using System.Net.Http.Json;
global using System.Runtime.InteropServices.JavaScript;
global using System.Text;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using System.Text.RegularExpressions;
global using ClientApp;
global using ClientApp.Components;
global using ClientApp.Extensions;
global using ClientApp.Interop;
global using ClientApp.Models;
global using ClientApp.Options;
global using ClientApp.Services;
global using Markdig;
global using Microsoft.AspNetCore.Components;
global using Microsoft.AspNetCore.Components.Routing;
global using Microsoft.AspNetCore.Components.Web;
global using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.JSInterop;
global using MudBlazor;
global using MudBlazor.Services;
global using Shared.Models;

[assembly: System.Runtime.Versioning.SupportedOSPlatform("browser")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ClientApp.Tests")]
