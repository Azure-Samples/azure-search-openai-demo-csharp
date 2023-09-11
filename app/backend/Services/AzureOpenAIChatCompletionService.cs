// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Services;

public sealed class AzureOpenAIChatCompletionService : ITextCompletion
{
    private readonly OpenAIClient _openAIClient;
    private readonly string _deployedModelName;

    public AzureOpenAIChatCompletionService(OpenAIClient openAIClient, IConfiguration config)
    {
        _openAIClient = openAIClient;

        var deployedModelName = config["AzureOpenAiChatGptDeployment"];
        ArgumentNullException.ThrowIfNullOrEmpty(deployedModelName);
        _deployedModelName = deployedModelName;
    }


    public async Task<string> CompleteAsync(
        string text, CompleteRequestSettings requestSettings, CancellationToken cancellationToken = default)
    {
        var option = new CompletionsOptions
        {
            MaxTokens = requestSettings.MaxTokens,
            FrequencyPenalty = Convert.ToSingle(requestSettings.FrequencyPenalty),
            PresencePenalty = Convert.ToSingle(requestSettings.PresencePenalty),
            Temperature = Convert.ToSingle(requestSettings.Temperature),
            Prompts = { text },
        };

        foreach (var stopSequence in requestSettings.StopSequences)
        {
            option.StopSequences.Add(stopSequence);
        }

        var response =
            await _openAIClient.GetCompletionsAsync(
                _deployedModelName, option, cancellationToken);
        return response.Value is Completions completions && completions.Choices.Count > 0
            ? completions.Choices[0].Text
            : throw new AIException(AIException.ErrorCodes.InvalidConfiguration, "completion not found");
    }
}
