// Copyright (c) Microsoft. All rights reserved.

namespace MauiBlazor.Services;

public class MauiSpeechSynthesisService(ITextToSpeech textToSpeech)
    : ISpeechSynthesisService, ITextToSpeechPreferencesListener
{
    private CancellationTokenSource? _cts;
    private Task? _speakTask;

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

    public void OnAvailableVoicesChanged(Func<Task> onVoicesChanged)
    {
        _ = onVoicesChanged;
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

    public void Speak(SpeechSynthesisUtterance utterance)
    {
        _cts?.Cancel();
        _cts = new();

        _speakTask = Task.Run(async () =>
        {
            var current = CultureInfo.CurrentUICulture.Name;

            var locales = await textToSpeech.GetLocalesAsync();
            var localeArray = locales.ToArray();
            var locale = localeArray.FirstOrDefault(l => current == $"{l.Language}-{l.Country}");
            if (locale is null)
            {
                // an exact match was not found, try just the lang
                var split = current.Split('-');
                if (split.Length is 1 or 2)
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

            await textToSpeech.SpeakAsync(utterance.Text, options, _cts.Token);
        }, _cts.Token);
    }

    public void UnsubscribeFromAvailableVoicesChanged()
    {
    }
}
