// Copyright (c) Microsoft. All rights reserved.

namespace MauiBlazor.Services;

public sealed class MauiSpeechRecognitionService(ISpeechToText speechToText) : ISpeechRecognitionService
{
    private SpeechRecognitionOperation? _current;

    public void CancelSpeechRecognition(bool isAborted)
    {
        _ = isAborted;
        _current?.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        _current?.Dispose();
        return ValueTask.CompletedTask;
    }

    public Task InitializeModuleAsync(bool logModuleDetails = true)
    {
        _ = logModuleDetails;
        return Task.CompletedTask;
    }

    public IDisposable RecognizeSpeech(string language, Action<string> onRecognized, Action<SpeechRecognitionErrorEvent>? onError = null, Action? onStarted = null, Action? onEnded = null)
    {
        return _current = new SpeechRecognitionOperation(speechToText, language, onRecognized, onError, onStarted, onEnded);
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
