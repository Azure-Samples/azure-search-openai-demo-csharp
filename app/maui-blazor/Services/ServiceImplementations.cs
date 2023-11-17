// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading;
using CommunityToolkit.Maui.Media;

namespace MauiBlazor.Services;

// NOTE: These service implementations are incomplete. Only a few parts are implemented
// for basic app functionality (for now).

public class MauiSessionStorageService : ISessionStorageService
{
    public double Length => 0;

    public void Clear()
    {
    }

    public TValue? GetItem<TValue>(string key, JsonSerializerOptions? options = null)
    {
        return default;
    }

    public string? Key(double index)
    {
        return default;
    }

    public void RemoveItem(string key)
    {
    }

    public void SetItem<TValue>(string key, TValue value, JsonSerializerOptions? options = null)
    {
    }
}

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

        _cts = null;
    }
}

public class MauiLocalStorageService : ILocalStorageService
{
    private readonly IPreferences _prefs;

    public MauiLocalStorageService(IPreferences prefs)
    {
        _prefs = prefs;
    }

    public double Length => 0;

    public void Clear() =>
        _prefs.Clear();

    public TValue? GetItem<TValue>(string key, JsonSerializerOptions? options = null)
    {
        // if the type is a nullable, then use the underlying type for parsing
        if (Nullable.GetUnderlyingType(typeof(TValue)) is Type under)
        {
            var get = _prefs.GetType().GetMethod("Get")!;
            var args = new object?[] { key, Activator.CreateInstance(under), default(string) };
            return (TValue?)get.MakeGenericMethod(under).Invoke(_prefs, args);
        }
        else
        {
            return _prefs.Get<TValue>(key, default);
        }
    }

    public string? Key(double index) => default;

    public void RemoveItem(string key) =>
        _prefs.Remove(key);

    public void SetItem<TValue>(string key, TValue value, JsonSerializerOptions? options = null)
    {
        // if the type is a nullable, then use the underlying type for parsing
        if (Nullable.GetUnderlyingType(typeof(TValue)) is Type under)
        {
            var set = _prefs.GetType().GetMethod("Set")!;
            var args = new object?[] { key, value, default(string) };
            set.MakeGenericMethod(under).Invoke(_prefs, args);
        }
        else
        {
            _prefs.Set<TValue>(key, value);
        }
    }
}

public class MauiSpeechRecognitionService : ISpeechRecognitionService
{
    private readonly ISpeechToText _speechToText;

    private SpeechRecognitionOperation? _current;

    public MauiSpeechRecognitionService(ISpeechToText speechToText)
    {
        _speechToText = speechToText;
    }

    public void CancelSpeechRecognition(bool isAborted)
    {
        _current?.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        _current?.Dispose();
        return ValueTask.CompletedTask;
    }

    public Task InitializeModuleAsync(bool logModuleDetails = true)
    {
        return Task.CompletedTask;
    }

    public IDisposable RecognizeSpeech(string language, Action<string> onRecognized, Action<SpeechRecognitionErrorEvent>? onError = null, Action? onStarted = null, Action? onEnded = null)
    {
        return _current = new SpeechRecognitionOperation(_speechToText, language, onRecognized, onError, onStarted, onEnded);
    }

    private class SpeechRecognitionOperation : IDisposable
    {
        private readonly CancellationTokenSource _cts = new();
        private readonly ISpeechToText _speechToText;
        private readonly string _language;
        private readonly Action<string> _onRecognized;
        private readonly Action<SpeechRecognitionErrorEvent>? _onError;
        private readonly Action? _onStarted;
        private readonly Action? _onEnded;

        public SpeechRecognitionOperation(ISpeechToText speechToText, string language, Action<string> onRecognized, Action<SpeechRecognitionErrorEvent>? onError, Action? onStarted, Action? onEnded)
        {
            _speechToText = speechToText;
            _language = language;
            _onRecognized = onRecognized;
            _onError = onError;
            _onStarted = onStarted;
            _onEnded = onEnded;

            StartAsync();
        }

        private async void StartAsync()
        {
            _onStarted?.Invoke();

            try
            {
                var isGranted = await _speechToText.RequestPermissions(_cts.Token);
                if (!isGranted)
                {
                    _onError?.Invoke(new SpeechRecognitionErrorEvent("Permissions Error", "Permissions were not granted."));
                    _onEnded?.Invoke();
                    return;
                }

                var culture = CultureInfo.GetCultureInfo(_language);
                var last = "";
                var recognitionResult = await _speechToText.ListenAsync(culture, new Progress<string>(rec => {
                    var current = last;
                    last = rec;
                    // nothing changed, so skip
                    if (rec.Length <= current.Length)
                    {
                        return;
                    }
                    // new words added, trim and pass just the new words
                    if (rec.Length >= current.Length)
                    {
                        rec = rec[current.Length..].Trim();
                    }
                    // only fire the event if there was actual new words
                    if (!string.IsNullOrWhiteSpace(rec))
                    {
                        _onRecognized?.Invoke(rec);
                    }
                }), _cts.Token);

                if (!recognitionResult.IsSuccessful)
                {
                    _onError?.Invoke(new SpeechRecognitionErrorEvent("Unknown Error",
                        $"Unable to recognize speech. Got: '{recognitionResult.Text}'. Ex: {recognitionResult.Exception}"));
                }
            }
            catch (Exception ex)
            {
                _onError?.Invoke(new SpeechRecognitionErrorEvent("Unknown Error", ex.Message));
            }

            _onEnded?.Invoke();
        }

        public void Dispose()
        {
            _cts.Cancel();
        }
    }
}
