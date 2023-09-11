// Copyright (c) Microsoft. All rights reserved.

namespace EmbedFunctions.Services;

public sealed class EmbedServiceFactory
{
    private readonly IEnumerable<IEmbedService> _embedServices =
        Array.Empty<IEmbedService>();

    public EmbedServiceFactory(IEnumerable<IEmbedService> embedServices) => _embedServices = embedServices;

    public IEmbedService GetEmbedService(EmbeddingType embeddingType) => embeddingType switch
    {
        EmbeddingType.AzureSearch =>
            _embedServices.OfType<AzureSearchEmbedService>().Single(),

        EmbeddingType.Pinecone =>
            _embedServices.OfType<PineconeEmbedService>().Single(),

        EmbeddingType.Qdrant =>
            _embedServices.OfType<QdrantEmbedService>().Single(),

        EmbeddingType.Milvus =>
            _embedServices.OfType<MilvusEmbedService>().Single(),

        _ => throw new ArgumentException(
            $"Unsupported embedding type: {embeddingType}", nameof(embeddingType))
    };
}
