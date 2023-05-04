// Copyright (c) Microsoft. All rights reserved.



namespace MinimalApi.Services;

internal sealed class SentenceEmbeddingService : IEmbeddingGeneration<string, float>
{
    private readonly Tokenizer _tokenizer;
    private readonly ITransformer _sbert;
    private readonly MLContext _mlContext;
    private PredictionEngine<ModelInput, ModelOutput>? _predictionEngine;

    public SentenceEmbeddingService()
    {
        _tokenizer = new Tokenizer(new Bpe("SBert/vocab.json", null, unknownToken: "[UNK]", continuingSubwordPrefix: "##", endOfWordSuffix: null));
        _mlContext = new MLContext(0);
        var onnx = _mlContext.Transforms.ApplyOnnxModel(modelFile: "SBert/sbert.onnx");
        _sbert = onnx.Fit(_mlContext.Data.LoadFromEnumerable(new List<ModelInput>()));
        _predictionEngine = _mlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(_sbert);
    }

    public Task<IList<Embedding<float>>> GenerateEmbeddingsAsync(IList<string> data, CancellationToken cancellationToken = default)
    {
        var tokens = data.Select(d => _tokenizer.Encode(d).Ids).ToArray();
        var embeddings = new List<Embedding<float>>();
        foreach (var token in tokens)
        {
            var chunkToken = token.Take(512);
            var output = _predictionEngine!.Predict(new ModelInput
            {
                Token = chunkToken.Select(i => (long)i).ToArray(), // max length of 512
                TokenTypes = chunkToken.Select(i => (long)0).ToArray(),
                AttentionMask = chunkToken.Select(i => (long)1).ToArray(),
            });
            embeddings.Add(new Embedding<float>(output!.Embedding));
        }

        return Task.FromResult<IList<Embedding<float>>>(embeddings);
    }

    private class ModelInput
    {
        [ColumnName("input_ids")]
        public long[] Token { get; set; } = Array.Empty<long>();

        [ColumnName("token_type_ids")]
        public long[] TokenTypes { get; set; } = Array.Empty<long>();

        [ColumnName("attention_mask")]
        public long[] AttentionMask { get; set; } = Array.Empty<long>();
    }

    private class ModelOutput
    {
        [ColumnName("pooler_output")]
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }
}
