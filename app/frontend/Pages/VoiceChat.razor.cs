// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Pages;

public sealed partial class VoiceChat : IDisposable
{
    private string _userPrompt = "";
    private bool _isRecognizingSpeech = false;
    private bool _isReceivingResponse = false;
    private bool _isReadingResponse = false;
    private string? _intermediateResponse = null;
    private IDisposable? _recognitionSubscription;
    private SpeechRecognitionErrorEvent? _errorEvent;
    private VoicePreferences? _voicePreferences;
    private HashSet<string> _responses = new();
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .ConfigureNewLine("\n")
        .UseAdvancedExtensions()
        .UseEmojiAndSmiley()
        .UseSoftlineBreakAsHardlineBreak()
        .Build();

    [Inject] public required OpenAIPromptQueue OpenAIPrompts { get; set; }
    [Inject] public required IDialogService Dialog { get; set; }
    [Inject] public required ISpeechRecognitionService SpeechRecognition { get; set; }
    [Inject] public required ISpeechSynthesisService SpeechSynthesis { get; set; }
    [Inject] public required ILocalStorageService LocalStorage { get; set; }
    [Inject] public required ISessionStorageService SessionStorage { get; set; }
    [Inject] public required IJSInProcessRuntime JavaScript { get; set; }
    [Inject] public required IStringLocalizer<VoiceChat> Localizer { get; set; }

    private string Prompt => Localizer[nameof(Prompt)];
    private string Save => Localizer[nameof(Save)];
    private string Speak => Localizer[nameof(Speak)];
    private string Stop => Localizer[nameof(Stop)];
    private string Chat => Localizer[nameof(Chat)];
    private string ChatPrompt => Localizer[nameof(ChatPrompt)];
    private string Ask => Localizer[nameof(Ask)];
    private string TTSPreferences => Localizer[nameof(TTSPreferences)];

    protected override void OnInitialized()
    {
        if (SessionStorage.GetItem<HashSet<string>>(
            "openai-prompt-responses") is { Count: > 0 } responses)
        {
            _responses = responses;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await SpeechRecognition.InitializeModuleAsync();
        }
    }

    private void OnSendPrompt()
    {
        if (_isReceivingResponse)
        {
            return;
        }

        _isReceivingResponse = true;

        OpenAIPrompts.Enqueue(
            _userPrompt,
            async response => await InvokeAsync(() =>
            {
                var (_, responseText, isComplete) = response;
                var promptWithResponseText = $"""
                > {_userPrompt}

                {responseText}
                """;
                var html = Markdown.ToHtml(promptWithResponseText, _pipeline);

                _intermediateResponse = html;

                if (isComplete)
                {
                    _responses.Add(_intermediateResponse);
                    SessionStorage.SetItem("openai-prompt-responses", _responses);

                    _intermediateResponse = null;
                    _isReadingResponse = true;

                    _voicePreferences = new VoicePreferences(LocalStorage);
                    var (voice, rate, isEnabled) = _voicePreferences;
                    if (isEnabled)
                    {
                        var utterance = new SpeechSynthesisUtterance
                        {
                            Rate = rate,
                            Text = responseText
                        };
                        if (voice is not null)
                        {
                            utterance.Voice = new SpeechSynthesisVoice
                            {
                                Name = voice
                            };
                        }
                        SpeechSynthesis.Speak(utterance, duration =>
                        {
                            _isReadingResponse = false;
                            StateHasChanged();
                        });
                    }
                }

                _isReceivingResponse = isComplete is false;
                if (isComplete)
                {
                    _userPrompt = "";
                }

                StateHasChanged();
            }));
    }

    protected override void OnAfterRender(bool firstRender) =>
        JavaScript.InvokeVoid("highlight");

    private void StopTalking()
    {
        SpeechSynthesis.Cancel();
        _isReadingResponse = false;
    }

    private void OnRecognizeSpeechClick()
    {
        if (_isRecognizingSpeech)
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

    private async Task ShowVoiceDialogAsync()
    {
        var dialog = await Dialog.ShowAsync<VoiceDialog>(title: TTSPreferences);
        var result = await dialog.Result;
        if (result is not { Canceled: true })
        {
            _voicePreferences = await dialog.GetReturnValueAsync<VoicePreferences>();
        }
    }

    private void OnStarted()
    {
        _isRecognizingSpeech = true;
        StateHasChanged();
    }

    private void OnEnded()
    {
        _isRecognizingSpeech = false;
        StateHasChanged();
    }

    private void OnError(SpeechRecognitionErrorEvent errorEvent)
    {
        _errorEvent = errorEvent;
        StateHasChanged();
    }

    private void OnRecognized(string transcript)
    {
        _userPrompt = _userPrompt switch
        {
            null => transcript,
            _ => $"{_userPrompt.Trim()} {transcript}".Trim()
        };

        StateHasChanged();
    }

    public void Dispose() => _recognitionSubscription?.Dispose();
}
