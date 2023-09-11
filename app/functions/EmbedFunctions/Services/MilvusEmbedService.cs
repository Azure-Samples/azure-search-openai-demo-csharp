// Copyright (c) Microsoft. All rights reserved.

namespace EmbedFunctions.Services;

internal sealed class MilvusEmbedService : IEmbedService
{
    public Task<bool> EmbedBlobAsync(Stream blobStream, string blobName) => throw new NotImplementedException();
}
