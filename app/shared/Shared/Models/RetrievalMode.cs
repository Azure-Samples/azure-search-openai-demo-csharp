// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Shared.Models;

/// <summary>
/// retrieval mode for azure search service
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RetrievalMode
{
    /// <summary>
    /// Text-only model, where only query will be used to retrieve the results
    /// </summary>
    Text = 0,

    /// <summary>
    /// Vector-only model, where only embeddings will be used to retrieve the results
    /// </summary>
    Vector,

    /// <summary>
    /// Text + Vector model, where both query and embeddings will be used to retrieve the results
    /// </summary>
    Hybrid,
}
