// Copyright (c) Microsoft. All rights reserved.

namespace Shared;

public enum EmbeddingType
{
    /// <summary>
    /// Embed using Azure AI Search (Vector).
    /// See <a href='https://learn.microsoft.com/azure/search/vector-search-overview'>https://learn.microsoft.com/azure/search/vector-search-overview</a>
    /// </summary>
    AzureSearch = 0,

    /// <summary>
    /// Embed using the Pinecone Vector Database.
    /// See <a href='https://www.pinecone.io'>https://www.pinecone.io</a>
    /// </summary>
    Pinecone = 1,

    /// <summary>
    /// Embed using the Qdrant Vector Database.
    /// See <a href='https://qdrant.tech'>https://qdrant.tech</a>
    /// </summary>
    Qdrant = 2,

    /// <summary>
    /// Embed using the Milvus Vector Database.
    /// See <a href='https://milvus.io'>https://milvus.io</a>
    /// </summary>
    Milvus = 3
};
