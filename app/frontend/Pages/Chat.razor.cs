// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Pages;

public sealed partial class Chat
{
    private string _userQuestion = "";
    private string _lastReferenceQuestion = "";
    private bool _isReceivingResponse = false;

    private readonly Dictionary<string, ApproachResponse?> _questionAndAnswerMap =
        new(StringComparer.OrdinalIgnoreCase);

    [Inject] public required IStringLocalizer<Chat> Localizer { get; set; }    

    [Inject] public required ISessionStorageService SessionStorage { get; set; }
    
    [Inject] public required ApiClient ApiClient { get; set; }

    [CascadingParameter(Name = nameof(Settings))]
    public required RequestSettingsOverrides Settings { get; set; }

    private string Prompt => Localizer[nameof(Prompt)];
    private string ChatTitle => Localizer[nameof(ChatTitle)];
    private string ChatPrompt => Localizer[nameof(ChatPrompt)];
    private string Ask => Localizer[nameof(Ask)];

    private Task OnAskQuestionAsync(string question)
    {
        _userQuestion = question;
        return OnAskClickedAsync();
    }

    private async Task OnAskClickedAsync()
    {
        _isReceivingResponse = true;
        _lastReferenceQuestion = _userQuestion;
        _questionAndAnswerMap[_userQuestion] = null;

        try
        {
            var history = _questionAndAnswerMap
                .Where(x => x.Value is not null)
                .Select(x => new ChatTurn(x.Key, x.Value!.Answer))
                .ToList();

            history.Add(new ChatTurn(_userQuestion));
            
            var request = new ChatRequest(history.ToArray(), Settings.Approach, Settings.Overrides);
            var result = await ApiClient.ChatConversationAsync(request);

            _questionAndAnswerMap[_userQuestion] = result.Response;
            if (result.IsSuccessful)
            {
                _userQuestion = "";
            }
        }
        finally
        {
            _isReceivingResponse = false;
        }
    }

    private void OnClearChat()
    {
        _userQuestion = _lastReferenceQuestion = "";
        _questionAndAnswerMap.Clear();
    }

    private async Task OnKeyUpAsync(KeyboardEventArgs args)
    {
        if (args is { Key: "Enter", ShiftKey: false })
        {
            await OnAskClickedAsync();
        }
    }
}
