// Copyright (c) Microsoft. All rights reserved.

namespace EmbedFunctions.Services;

public interface IEmbedService
{
    /// <summary>
    /// Embeds the given blob into the embedding service.
    /// </summary>
    /// <param name="blobStream">The stream from the blob to embed.</param>
    /// <param name="blobName">The name of the blob.</param>
    /// <returns>
    /// An asynchronous operation that yields <c>true</c>
    /// when successfully embedded, otherwise <c>false</c>.
    /// </returns>
    Task<bool> EmbedBlobAsync(
        Stream blobStream,
        string blobName);
}
