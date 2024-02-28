// Copyright (c) Microsoft. All rights reserved.

namespace SharedWebComponents.Components;

public sealed partial class VoiceDialog : IDisposable
{
    private SpeechSynthesisVoice[] _voices = [];
    private readonly IList<double> _voiceSpeeds =
        Enumerable.Range(0, 12).Select(i => (i + 1) * .25).ToList();
    private VoicePreferences? _voicePreferences;
    private RequestVoiceState _state;

    [Inject] public required ISpeechSynthesisService SpeechSynthesis { get; set; }

    [Inject] public required ITextToSpeechPreferencesListener VoiceChangesListener { get; set; }

    [Inject] public required ILocalStorageService LocalStorage { get; set; }

    [CascadingParameter] public required MudDialogInstance Dialog { get; set; }

    protected override async Task OnInitializedAsync()
    {
        _state = RequestVoiceState.RequestingVoices;

        await GetVoicesAsync();

        VoiceChangesListener.OnAvailableVoicesChanged(() => GetVoicesAsync(true));

        _voicePreferences = new VoicePreferences(LocalStorage);

        if (_voicePreferences.Voice is null &&
            _voices.FirstOrDefault(voice => voice.Default) is { } voice)
        {
            _voicePreferences.Voice = voice.Name;
        }
    }

    private async Task GetVoicesAsync(bool isFromCallback = false)
    {
        _voices = await SpeechSynthesis.GetVoicesAsync();
        if (_voices is { } && isFromCallback)
        {
            StateHasChanged();
        }

        if (_voices is { Length: > 0 })
        {
            _state = RequestVoiceState.FoundVoices;
        }
    }

    private void OnValueChanged(string selectedVoice) => _voicePreferences = _voicePreferences! with
    {
        Voice = selectedVoice
    };

    private void OnSaveVoiceSelection() => Dialog.Close(DialogResult.Ok(_voicePreferences));

    private void OnCancel() => Dialog.Close(DialogResult.Ok(_voicePreferences));

    public void Dispose()
    {
        VoiceChangesListener.UnsubscribeFromAvailableVoicesChanged();
    }
}

internal enum RequestVoiceState
{
    RequestingVoices,
    FoundVoices,
    Error
};
