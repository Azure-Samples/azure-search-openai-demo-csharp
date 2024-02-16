// Copyright (c) Microsoft. All rights reserved.

namespace MauiBlazor.Services;

public class MauiSpeechSynthesisService : ISpeechSynthesisService
{
    private CancellationTokenSource? _cts;

    public bool Paused => throw new NotImplementedException();

    public bool Pending => throw new NotImplementedException();

    public bool Speaking => _cts != null;

    public void Cancel()
    {
        _cts?.Cancel();
        _cts = null;
    }

    public ValueTask<SpeechSynthesisVoice[]> GetVoicesAsync()
    {
        var voice = new SpeechSynthesisVoice
        {
            Name = "Default Voice"
        };

        return ValueTask.FromResult<SpeechSynthesisVoice[]>([voice]);
    }

    public void Pause()
    {
        Cancel();

        // TODO: support pause & resume
    }

    public void Resume()
    {
        // TODO: support pause & resume
    }

    public async void Speak(SpeechSynthesisUtterance utterance)
    {
        _cts = new();

        var current = CultureInfo.CurrentUICulture.Name;

        var locales = await TextToSpeech.Default.GetLocalesAsync();
        var localeArray = locales.ToArray();
        var locale = localeArray.FirstOrDefault(l => current == $"{l.Language}-{l.Country}");
        if (locale is null)
        {
            // an exact match was not found, try just the lang
            var split = current.Split('-');
            if (split.Length == 1 || split.Length == 2)
            {
                // try the first part (or the whole thing if it is just lang)
                locale = localeArray.FirstOrDefault(l => split[0] == $"{l.Language}");
            }
            else
            {
                // just go with the first one
                locale = localeArray.FirstOrDefault();
            }
        }

        var options = new SpeechOptions
        {
            Locale = locale
        };
        await TextToSpeech.Default.SpeakAsync(utterance.Text, options, _cts.Token);
    }
}
