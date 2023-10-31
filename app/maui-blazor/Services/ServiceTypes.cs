// Copyright (c) Microsoft. All rights reserved.

namespace MauiBlazor.Services;

// NOTE: These interfaces, types, and methods are here to mimic the patterns used in the Blazor WebAssembly
// app. These come from various 'blazorators' files at https://github.com/IEvangelist/blazorators, but
// are simplified to just the definitions.

public interface ILocalStorageService
{
    double Length { get; }
    void Clear();
    TValue? GetItem<TValue>(string key, JsonSerializerOptions? options = null);
    string? Key(double index);
    void RemoveItem(string key);
    void SetItem<TValue>(string key, TValue value, JsonSerializerOptions? options = null);
}

public interface ISessionStorageService
{
    double Length { get; }
    void Clear();
    TValue? GetItem<TValue>(string key, JsonSerializerOptions? options = null);
    string? Key(double index);
    void RemoveItem(string key);
    void SetItem<TValue>(string key, TValue value, JsonSerializerOptions? options = null);
}

public interface ISpeechRecognitionService : IAsyncDisposable
{
    Task InitializeModuleAsync(bool logModuleDetails = true);
    void CancelSpeechRecognition(bool isAborted);
    IDisposable RecognizeSpeech(string language, Action<string> onRecognized, Action<SpeechRecognitionErrorEvent>? onError = null, Action? onStarted = null, Action? onEnded = null);
}

public interface ISpeechSynthesisService
{
    bool Paused { get; }
    bool Pending { get; }
    bool Speaking { get; }
    void Cancel();
    ValueTask<SpeechSynthesisVoice[]> GetVoicesAsync();
    void Pause();
    void Resume();
    void Speak(SpeechSynthesisUtterance utterance);
}
public class SpeechSynthesisUtterance
{
    [JsonPropertyName("lang")]
    public string Lang { get; set; }

    [JsonPropertyName("pitch")]
    public double Pitch { get; set; }

    [JsonPropertyName("rate")]
    public double Rate { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("voice")]
    public SpeechSynthesisVoice? Voice { get; set; }

    [JsonPropertyName("volume")]
    public double Volume { get; set; }
}

public record class SpeechRecognitionErrorEvent(
    [property: JsonPropertyName("error")] string Error,
    [property: JsonPropertyName("message")] string Message);

public class SpeechSynthesisVoice
{
    [JsonPropertyName("default")]
    public bool Default { get; set; }

    [JsonPropertyName("lang")]
    public string Lang { get; set; }

    [JsonPropertyName("localService")]
    public bool LocalService { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("voiceURI")]
    public string VoiceURI { get; set; }
}

public static class SpeechSynthesisServiceExtensions
{
    public static void Speak(
        this ISpeechSynthesisService service,
        SpeechSynthesisUtterance utterance,
        Action<double> onUtteranceEnded)
    {
    }

    [JSInvokable(nameof(OnUtteranceEnded))]
    public static void OnUtteranceEnded(
        string text, double elapsedTimeSpokenInMilliseconds)
    {
    }

    public static void OnVoicesChanged(
        this ISpeechSynthesisService service,
        Func<Task> onVoicesChanged)
    {
    }

    [JSInvokable(nameof(VoicesChangedAsync))]
    public static async Task VoicesChangedAsync(string guid)
    {
    }

    public static void UnsubscribeFromVoicesChanged(
        this ISpeechSynthesisService service)
    {
    }
}
