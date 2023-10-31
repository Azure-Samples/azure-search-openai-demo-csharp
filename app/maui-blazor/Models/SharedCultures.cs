// Copyright (c) Microsoft. All rights reserved.

namespace MauiBlazor.Models;

public record class SharedCultures
{
    [JsonPropertyName("translation")]
    public required IDictionary<string, AzureCulture> AvailableCultures { get; set; }
}
