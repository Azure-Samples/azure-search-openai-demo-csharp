// Copyright (c) Microsoft. All rights reserved.

using EmbedFunctions.Services.Interfaces;

namespace EmbedFunctions.Services;

internal sealed class MilvusEmbedService : IEmbedService
{
    public Task<bool> EmbedBlobAsync(Stream blobStream, string blobName) => throw new NotImplementedException();
}
