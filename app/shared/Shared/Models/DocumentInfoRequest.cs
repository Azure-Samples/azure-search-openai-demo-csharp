// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Shared.Models;

public class DocumentInfoRequest(
    )
{
    [property: JsonPropertyName("content")]
    public MultipartFormDataContent Content { get; set; }
    [property: JsonPropertyName("category")]
    public string Category { get; set; }
}
