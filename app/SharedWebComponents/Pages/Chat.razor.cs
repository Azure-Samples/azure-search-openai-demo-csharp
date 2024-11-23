// Copyright (c) Microsoft. All rights reserved.

namespace SharedWebComponents.Pages;
using Microsoft.AspNetCore.SignalR.Client;
using System.Text.Json;

public sealed partial class Chat : IAsyncDisposable
{
    private string _userQuestion = "";
    private UserQuestion _currentQuestion;
    private string _lastReferenceQuestion = "";
    private bool _isReceivingResponse = false;
    private HubConnection? _hubConnection;
    private string _streamingResponse = "";

    private readonly Dictionary<UserQuestion, ChatAppResponseOrError?> _questionAndAnswerMap = [];

    [Inject] public required ISessionStorageService SessionStorage { get; set; }

    [Inject] public required ApiClient ApiClient { get; set; }

    [Inject] public required NavigationManager NavigationManager { get; set; }

    [CascadingParameter(Name = nameof(Settings))]
    public required RequestSettingsOverrides Settings { get; set; }

    [CascadingParameter(Name = nameof(IsReversed))]
    public required bool IsReversed { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await ConnectToHub();
    }

    private async Task ConnectToHub()
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            return;
        }

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(NavigationManager.ToAbsoluteUri("/chat-hub"))
            .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30) })
            .Build();

        _hubConnection.On<string>("ReceiveMessage", (message) =>
        {
            try
            {
                if (_currentQuestion != default)
                {
                    var streamingMessage = JsonSerializer.Deserialize<StreamingMessage>(message);
                    if (streamingMessage?.Content == null) return;

                    switch (streamingMessage.Type.ToLowerInvariant())
                    {
                        case "content":
                            _streamingResponse += streamingMessage.Content;
                            UpdateAnswerInMap(_streamingResponse);
                            break;

                        case "thoughts":
                            if (streamingMessage.Content is JsonElement thoughtsElement)
                            {
                                var thoughts = thoughtsElement.EnumerateArray()
                                    .Select(t => new Thoughts(
                                        t.GetProperty("Title").GetString()!,
                                        t.GetProperty("Description").GetString()!))
                                    .ToArray();
                                UpdateThoughtsInMap(thoughts);
                            }
                            break;

                        case "followup":
                            if (streamingMessage.Content is JsonElement followupElement)
                            {
                                var followupQuestions = followupElement.EnumerateArray()
                                    .Select(q => q.GetString()!)
                                    .ToArray();
                                UpdateFollowupQuestionsInMap(followupQuestions);
                            }
                            break;

                        case "supporting":
                            if (streamingMessage.Content is JsonElement supportingElement)
                            {
                                var supportingContent = supportingElement.Deserialize<SupportingContentRecord[]>();
                                if (supportingContent != null)
                                {
                                    UpdateSupportingContentInMap(supportingContent);
                                }
                            }
                            break;

                        case "images":
                            if (streamingMessage.Content is JsonElement imagesElement)
                            {
                                var images = imagesElement.Deserialize<SupportingImageRecord[]>();
                                if (images != null)
                                {
                                    UpdateImagesInMap(images);
                                }
                            }
                            break;

                        case "complete":
                            if (streamingMessage.Content is JsonElement completeElement)
                            {
                                var finalResponse = completeElement.Deserialize<ChatAppResponseOrError>();
                                if (finalResponse != null)
                                {
                                    _questionAndAnswerMap[_currentQuestion] = finalResponse;
                                    _userQuestion = "";
                                    _currentQuestion = default;
                                }
                            }
                            break;
                    }
                    StateHasChanged();
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error deserializing response: {ex.Message}");
            }
        });

        try
        {
            await _hubConnection.StartAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting SignalR connection: {ex.Message}");
        }
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
        _streamingResponse = "";

        try
        {
            var history = _questionAndAnswerMap
                .Where(x => x.Value?.Choices is { Length: > 0 })
                .SelectMany(x => new ChatMessage[] {
                    new ChatMessage("user", x.Key.Question),
                    new ChatMessage("assistant", x.Value!.Choices[0].Message.Content)
                })
                .ToList();

            history.Add(new ChatMessage("user", _userQuestion));

            var request = new ChatRequest([.. history], Settings.Overrides);

            if (Settings.Overrides.UseStreaming && _hubConnection?.State == HubConnectionState.Connected)
            {
                try
                {
                    await _hubConnection.InvokeAsync("SendChatRequest", request);
                }
                catch (Exception ex)
                {
                    _questionAndAnswerMap[_currentQuestion] = new ChatAppResponseOrError(
                        Array.Empty<ResponseChoice>(),
                        $"Error: {ex.Message}");
                }
            }
            else
            {
                var result = await ApiClient.ChatConversationAsync(request);
                _questionAndAnswerMap[_currentQuestion] = result.Response;
            }

            if (_questionAndAnswerMap[_currentQuestion]?.Error == null)
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

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
        }
    }

    private async Task OnAskQuestionAsync(string question)
    {
        _userQuestion = question;
        await OnAskClickedAsync();
    }

    private async Task OnStreamingToggled(bool isEnabled)
    {
        if (isEnabled)
        {
            await ConnectToHub();
        }
        else
        {
            await DisconnectFromHub();
        }
    }

    private async Task DisconnectFromHub()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }

    private void UpdateAnswerInMap(string answer)
    {
        var currentResponse = _questionAndAnswerMap[_currentQuestion];
        var choice = currentResponse?.Choices.FirstOrDefault();
        var context = choice?.Context ?? new ResponseContext(null, null, Array.Empty<string>(), Array.Empty<Thoughts>());

        _questionAndAnswerMap[_currentQuestion] = new ChatAppResponseOrError(new[] {
            new ResponseChoice(
                Index: 0,
                Message: new ResponseMessage("assistant", answer),
                Context: context,
                CitationBaseUrl: choice?.CitationBaseUrl ?? "")
        });
    }

    private void UpdateThoughtsInMap(Thoughts[] thoughts)
    {
        var currentResponse = _questionAndAnswerMap[_currentQuestion];
        if (currentResponse?.Choices.FirstOrDefault() is { } choice)
        {
            var context = new ResponseContext(
                choice.Context.DataPointsContent,
                choice.Context.DataPointsImages,
                choice.Context.FollowupQuestions,
                thoughts);

            _questionAndAnswerMap[_currentQuestion] = new ChatAppResponseOrError(new[] {
                choice with { Context = context }
            });
        }
    }

    private void UpdateFollowupQuestionsInMap(string[] followupQuestions)
    {
        var currentResponse = _questionAndAnswerMap[_currentQuestion];
        if (currentResponse?.Choices.FirstOrDefault() is { } choice)
        {
            var context = new ResponseContext(
                choice.Context.DataPointsContent,
                choice.Context.DataPointsImages,
                followupQuestions,
                choice.Context.Thoughts);

            _questionAndAnswerMap[_currentQuestion] = new ChatAppResponseOrError(new[] {
                choice with { Context = context }
            });
        }
    }

    private void UpdateSupportingContentInMap(SupportingContentRecord[] supportingContent)
    {
        var currentResponse = _questionAndAnswerMap[_currentQuestion];
        if (currentResponse?.Choices.FirstOrDefault() is { } choice)
        {
            var context = new ResponseContext(
                supportingContent,
                choice.Context.DataPointsImages,
                choice.Context.FollowupQuestions,
                choice.Context.Thoughts);

            _questionAndAnswerMap[_currentQuestion] = new ChatAppResponseOrError(new[] {
                choice with { Context = context }
            });
        }
    }

    private void UpdateImagesInMap(SupportingImageRecord[] images)
    {
        var currentResponse = _questionAndAnswerMap[_currentQuestion];
        if (currentResponse?.Choices.FirstOrDefault() is { } choice)
        {
            var context = new ResponseContext(
                choice.Context.DataPointsContent,
                images,
                choice.Context.FollowupQuestions,
                choice.Context.Thoughts);

            _questionAndAnswerMap[_currentQuestion] = new ChatAppResponseOrError(new[] {
                choice with { Context = context }
            });
        }
    }
}

internal class StreamingMessage
{
    public string Type { get; set; } = "";
    public object? Content { get; set; }
}
