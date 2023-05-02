// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Models;

public record class VoicePreferences
{
    private const string PreferredVoiceKey = "preferred-voice";
    private const string PreferredSpeedKey = "preferred-speed";
    private const string TtsIsEnabledKey = "tts-is-enabled";

    private string? _voice;
    private double? _rate;
    private bool? _isEnabled;

    private readonly ILocalStorageService _storage;

    public VoicePreferences(ILocalStorageService storage) => _storage = storage;

    public string? Voice
    {
        get => _voice ??= _storage.GetItem<string>(PreferredVoiceKey);
        set
        {
            if (_voice != value && value is not null)
            {
                _voice = value;
                _storage.SetItem<string>(PreferredVoiceKey, value);
            }
        }
    }

    public double Rate
    {
        get => _rate ??= _storage.GetItem<double>(PreferredSpeedKey) is double rate
            && rate > 0 ? rate : 1;
        set
        {
            if (_rate != value)
            {
                _rate = value;
                _storage.SetItem<double>(PreferredSpeedKey, value);
            }
        }
    }

    public bool IsEnabled
    {
        get => _isEnabled ??= (_storage.GetItem<bool?>(TtsIsEnabledKey) is { } enabled
            && enabled);
        set
        {
            if (_isEnabled != value)
            {
                _isEnabled = value;
                _storage.SetItem<bool?>(TtsIsEnabledKey, value);
            }
        }
    }

    public void Deconstruct(out string? voice, out double rate, out bool isEnabled) =>
        (voice, rate, isEnabled) = (Voice, Rate, IsEnabled);
}
