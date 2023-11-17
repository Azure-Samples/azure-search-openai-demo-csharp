// Copyright (c) Microsoft. All rights reserved.

namespace SharedWebComponents.Models;

public record class SharedCultures
{
    [JsonPropertyName("translation")]
    public required IDictionary<string, AzureCulture> AvailableCultures { get; set; }
}
