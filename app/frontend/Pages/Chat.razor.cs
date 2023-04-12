// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Pages;

public sealed partial class Chat
{
    private string _userQuestion = "";
    private string _lastReferenceQuestion = "";
    private bool _isReceivingResponse = false;

    private readonly Dictionary<string, AskResponse?> _questionAndAnswerMap =
        new(StringComparer.OrdinalIgnoreCase);

    private Approach _approach;

    [Inject] public required IStringLocalizer<Chat> Localizer { get; set; }    

    [Inject] public required ISessionStorageService SessionStorage { get; set; }
    
    [Inject] public required HttpClient ApiClient { get; set; }

    private string Prompt => Localizer[nameof(Prompt)];
    private string ChatTitle => Localizer[nameof(ChatTitle)];
    private string ChatPrompt => Localizer[nameof(ChatPrompt)];
    private string Ask => Localizer[nameof(Ask)];

    protected override void OnInitialized()
    {
        _approach =
            SessionStorage.GetItem<Approach?>(StorageKeys.ClientApproach) is { } approach
                ? approach
                : Approach.RetrieveThenRead;
    }

    private async Task OnExampleClickedAsync(string exampleText)
    {
        _userQuestion = exampleText;
        await OnAskClickedAsync();
    }

    private async Task OnAskClickedAsync()
    {
        _isReceivingResponse = true;
        _lastReferenceQuestion = _userQuestion;
        _questionAndAnswerMap[_userQuestion] = null;

        try
        {
            var request = new AskRequest(
                _userQuestion, _approach, new());
            var json = JsonSerializer.Serialize(
                request,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));

            using var body = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await ApiClient.PostAsync("api/ask", body);

            if (response.IsSuccessStatusCode)
            {
                var answer = await response.Content.ReadFromJsonAsync<AskResponse>();
                if (answer is not null)
                {
                    _questionAndAnswerMap[_userQuestion] = answer;
                    _userQuestion = "";
                }
            }
            else
            {
                // TODO: error
            }
        }
        finally
        {
            _isReceivingResponse = false;
        }
    }
}
