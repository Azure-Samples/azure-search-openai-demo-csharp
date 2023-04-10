﻿// Copyright (c) Microsoft. All rights reserved.

namespace Backend.Services;

internal sealed class AzureOpenAITextCompletionService : ITextCompletion
{
    private readonly OpenAIClient _openAIClient;
    private readonly string _deployedModelName;

    public AzureOpenAITextCompletionService(OpenAIClient openAIClient, string deployedModelName)
    {
        _openAIClient = openAIClient;
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
        if (response.Value is Completions completions && completions.Choices.Count > 0)
        {
            return completions.Choices[0].Text;
        }
        else
        {
            throw new AIException(AIException.ErrorCodes.InvalidConfiguration, "completion not found");
        }
    }
}
