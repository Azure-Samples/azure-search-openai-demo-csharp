// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Components;

public sealed partial class VoiceDialog : IDisposable
{
    private SpeechSynthesisVoice[] _voices = Array.Empty<SpeechSynthesisVoice>();
    private readonly IList<double> _voiceSpeeds =
        Enumerable.Range(0, 12).Select(i => (i + 1) * .25).ToList();
    private VoicePreferences? _voicePreferences;
    private RequestVoiceState _state;

    [Inject] public required ISpeechSynthesisService SpeechSynthesis { get; set; }

    [Inject] public required ILocalStorageService LocalStorage { get; set; }

    [CascadingParameter] public required MudDialogInstance Dialog { get; set; }

    [Inject] public required IStringLocalizer<VoiceDialog> Localizer { get; set; }

    public string ClientVoicesMessage => Localizer[nameof(ClientVoicesMessage)];
    public string Voice => Localizer[nameof(Voice)];
    public string VoiceSpeed => Localizer[nameof(VoiceSpeed)];
    public string TTSEnabled => Localizer[nameof(TTSEnabled)];
    public string LoadVoicesError => Localizer[nameof(LoadVoicesError)];
    public string Cancel => Localizer[nameof(Cancel)];
    public string Save => Localizer[nameof(Save)];

    protected override async Task OnInitializedAsync()
    {
        _state = RequestVoiceState.RequestingVoices;

        await GetVoicesAsync();
        SpeechSynthesis.OnVoicesChanged(() => GetVoicesAsync(true));

        _voicePreferences = new VoicePreferences(LocalStorage);
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

    public void Dispose() => SpeechSynthesis.UnsubscribeFromVoicesChanged();
}

internal enum RequestVoiceState
{
    RequestingVoices,
    FoundVoices,
    Error
}
