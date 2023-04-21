// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Components;

public sealed partial class VoiceTextInput : IDisposable
{
    private string? _value;
    private bool _isListening = false;

    private string MicIcon => _isListening
        ? Icons.Material.Filled.MicOff
        : Icons.Material.Filled.Mic;

    private IDisposable? _recognitionSubscription;

    [Parameter] public EventCallback OnEnterKeyPressed { get; set; }

    [Parameter]
#pragma warning disable BL0007 // Component parameters should be auto properties
    public required string? Value
#pragma warning restore BL0007 // This is required for proper event propagation
    {
        get => _value;
        set
        {
            if (_value == value)
            {
                return;
            }

            _value = value;
            ValueChanged.InvokeAsync(value);
        }
    }
    [Parameter] public EventCallback<string?> ValueChanged { get; set; }
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public string Label { get; set; } = "";
    [Parameter] public string Placeholder { get; set; } = "";
    [Parameter] public string HelperText { get; set; } = "Use Shift + Enter for new lines.";
    [Parameter] public string Icon { get; set; } = Icons.Material.Filled.VoiceChat;
    [Inject] public required ISpeechRecognitionService SpeechRecognition { get; set; }
    [Inject] public required ILogger<VoiceTextInput> Logger { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await SpeechRecognition.InitializeModuleAsync();
        }
    }

    private void OnMicClicked()
    {
        if (_isListening)
        {
            SpeechRecognition.CancelSpeechRecognition(false);
        }
        else
        {
            var bcp47Tag = CultureInfo.CurrentUICulture.Name;

            _recognitionSubscription?.Dispose();
            _recognitionSubscription = SpeechRecognition.RecognizeSpeech(
                bcp47Tag,
                OnRecognized,
                OnError,
                OnStarted,
                OnEnded);
        }
    }

    private async Task OnKeyUpAsync(KeyboardEventArgs args)
    {
        if (args is { Key: "Enter", ShiftKey: false } &&
            OnEnterKeyPressed.HasDelegate)
        {
            await OnEnterKeyPressed.InvokeAsync();
        }
    }

    private void OnStarted()
    {
        _isListening = true;
        StateHasChanged();
    }

    private void OnEnded()
    {
        _isListening = false;
        StateHasChanged();
    }

    private void OnError(SpeechRecognitionErrorEvent errorEvent)
    {
        Logger.LogWarning(
            "{Error}: {Message}", errorEvent.Error, errorEvent.Message);

        StateHasChanged();
    }

    private void OnRecognized(string transcript)
    {
        Value = Value switch
        {
            null => transcript,
            _ => $"{Value.Trim()} {transcript}".Trim()
        };

        StateHasChanged();
    }

    public void Dispose() => _recognitionSubscription?.Dispose();
}
