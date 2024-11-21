// Copyright (c) Microsoft. All rights reserved.

namespace SharedWebComponents.Pages;
using System.Text;

public sealed partial class Chat
{
    private string _userQuestion = "";
    private UserQuestion _currentQuestion;
    private string _lastReferenceQuestion = "";
    private bool _isReceivingResponse = false;
    private bool _useStreaming = false;

    private readonly Dictionary<UserQuestion, ChatAppResponseOrError?> _questionAndAnswerMap = [];

    [Inject] public required ISessionStorageService SessionStorage { get; set; }

    [Inject] public required ApiClient ApiClient { get; set; }

    [CascadingParameter(Name = nameof(Settings))]
    public required RequestSettingsOverrides Settings { get; set; }

    [CascadingParameter(Name = nameof(IsReversed))]
    public required bool IsReversed { get; set; }

    private Task OnAskQuestionAsync(string question)
    {
        _userQuestion = question;
        return OnAskClickedAsync();
    }

    private async Task OnAskClickedAsync()
    {
        if (string.IsNullOrWhiteSpace(_userQuestion))
        {
            return;
        }

        _isReceivingResponse = true;
        _lastReferenceQuestion = _userQuestion;
        _currentQuestion = new(_userQuestion, DateTime.Now);
        _questionAndAnswerMap[_currentQuestion] = null;

        try
        {
            var history = _questionAndAnswerMap
                .Where(x => x.Value?.Choices is { Length: > 0 })
                .SelectMany(x => new ChatMessage[] { new ChatMessage("user", x.Key.Question), new ChatMessage("assistant", x.Value!.Choices[0].Message.Content) })
                .ToList();

            history.Add(new ChatMessage("user", _userQuestion));

            var request = new ChatRequest([.. history], Settings.Overrides);

            if (_useStreaming)
            {
                var streamingResponse = new StringBuilder();
                try
                {
                    await foreach (var chunk in await ApiClient.PostStreamingRequestAsync(request, "api/chat/stream"))
                    {
                        streamingResponse.Append(chunk.Text);

                        _questionAndAnswerMap[_currentQuestion] = new ChatAppResponseOrError(
                            new[] {
                                new ResponseChoice(0,
                                    new ResponseMessage("assistant", streamingResponse.ToString()),
                                    null, null)
                            },
                            null);

                        StateHasChanged();

                        await Task.Delay(10);
                    }
                }
                catch (Exception ex)
                {
                    _questionAndAnswerMap[_currentQuestion] = new ChatAppResponseOrError(
                        Array.Empty<ResponseChoice>(),
                        ex.Message);
                }
            }
            else
            {
                var result = await ApiClient.ChatConversationAsync(request);
                _questionAndAnswerMap[_currentQuestion] = result.Response;
            }

            if (_questionAndAnswerMap[_currentQuestion]?.Error is null)
            {
                _userQuestion = "";
                _currentQuestion = default;
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
        _currentQuestion = default;
        _questionAndAnswerMap.Clear();
    }
}
