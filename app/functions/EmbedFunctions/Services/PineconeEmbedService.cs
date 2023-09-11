// Copyright (c) Microsoft. All rights reserved.

namespace EmbedFunctions.Services;

internal sealed class PineconeEmbedService : IEmbedService
{
    public Task<bool> EmbedBlobAsync(Stream blobStream, string blobName) => throw new NotImplementedException(
            "Pinecone embedding isn't implemented.");
}
