// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Services;

internal sealed class SentenceEmbeddingService : IEmbeddingGeneration<string, float>
{
    private readonly MLContext _mlContext;
    private PredictionEngine<Input, Output>? _predictionEngine;

    public SentenceEmbeddingService(IEnumerable<CorpusRecord> corpusToTrain)
    {
        _mlContext = new MLContext(0);
        Train(corpusToTrain.Select(c => c.Text));
    }

    private void Train(IEnumerable<string> inputs)
    {
        var featurizeTextOption = new TextFeaturizingEstimator.Options
        {
            StopWordsRemoverOptions = new StopWordsRemovingEstimator.Options
            {
                Language = TextFeaturizingEstimator.Language.English,
            }
        };
        var textFeaturizer = _mlContext.Transforms.Text.FeaturizeText(outputColumnName: "Embedding", featurizeTextOption, "Text");
        var model = textFeaturizer.Fit(_mlContext.Data.LoadFromEnumerable(inputs.Select(i => new { Text = i })));
        _predictionEngine = _mlContext.Model.CreatePredictionEngine<Input, Output>(model);
    }

    public Task<IList<Embedding<float>>> GenerateEmbeddingsAsync(IList<string> data, CancellationToken cancellationToken = default)
    {
        var outputs = data.Select(i => _predictionEngine!.Predict(new Input { Text = i })).ToList();
        var embeddings = outputs.Select(o => new Embedding<float>(o.Embedding!)).ToList();
        return Task.FromResult<IList<Embedding<float>>>(embeddings);
    }

    private class Input
    {
        public string Text { get; set; } = string.Empty;
    }

    private class Output
    {
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }
}
