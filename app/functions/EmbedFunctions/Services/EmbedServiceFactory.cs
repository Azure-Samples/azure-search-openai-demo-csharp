// Copyright (c) Microsoft. All rights reserved.

namespace EmbedFunctions.Services;

public sealed class EmbedServiceFactory(IEnumerable<IEmbedService> embedServices)
{
    public IEmbedService GetEmbedService(EmbeddingType embeddingType) => embeddingType switch
    {
        EmbeddingType.AzureSearch =>
            embedServices.OfType<AzureSearchEmbedService>().Single(),

        EmbeddingType.Pinecone =>
            embedServices.OfType<PineconeEmbedService>().Single(),

        EmbeddingType.Qdrant =>
            embedServices.OfType<QdrantEmbedService>().Single(),

        EmbeddingType.Milvus =>
            embedServices.OfType<MilvusEmbedService>().Single(),

        _ => throw new ArgumentException(
            $"Unsupported embedding type: {embeddingType}", nameof(embeddingType))
    };
}
