// Copyright (c) Microsoft. All rights reserved.

namespace MauiBlazor.Services;

// NOTE: These service implementations are incomplete. Only a few parts are implemented
// for basic app functionality (for now).

public class SessionStorageService : ISessionStorageService
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


public class SpeechSynthesisService : ISpeechSynthesisService
{
    public bool Paused => false;

    public bool Pending => false;

    public bool Speaking => false;

    public void Cancel()
    {
    }

    public ValueTask<SpeechSynthesisVoice[]> GetVoicesAsync()
    {
        return ValueTask.FromResult<SpeechSynthesisVoice[]>(null);
    }

    public void Pause()
    {
    }

    public void Resume()
    {
    }

    public void Speak(SpeechSynthesisUtterance utterance)
    {
    }
}

public class LocalStorageService : ILocalStorageService
{
    public double Length => 0;

    public void Clear()
    {
    }

    public TValue? GetItem<TValue>(string key, JsonSerializerOptions? options = null)
    {
        return Preferences.Default.Get<TValue>(key, default(TValue));
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
        Preferences.Default.Set<TValue>(key, value);
    }
}

public class SpeechRecognitionService : ISpeechRecognitionService
{
    public void CancelSpeechRecognition(bool isAborted)
    {
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    public Task InitializeModuleAsync(bool logModuleDetails = true)
    {
        return Task.CompletedTask;
    }

    public IDisposable RecognizeSpeech(string language, Action<string> onRecognized, Action<SpeechRecognitionErrorEvent>? onError = null, Action? onStarted = null, Action? onEnded = null)
    {
        return default;
    }
}
