// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Pages;

public sealed partial class Chat
{
    private string _userQuestion = "";
    private string _lastReferenceQuestion = "";
    private bool _isReceivingResponse = false;

    private readonly Dictionary<string, ApproachResponse?> _questionAndAnswerMap =
        new(StringComparer.OrdinalIgnoreCase);

    private Approach _approach;

    [Inject] public required IStringLocalizer<Chat> Localizer { get; set; }    

    [Inject] public required ISessionStorageService SessionStorage { get; set; }
    
    [Inject] public required HttpClient ApiClient { get; set; }

    [CascadingParameter] public RequestOverrides? Overrides { get; set; }

    private string Prompt => Localizer[nameof(Prompt)];
    private string ChatTitle => Localizer[nameof(ChatTitle)];
    private string ChatPrompt => Localizer[nameof(ChatPrompt)];
    private string Ask => Localizer[nameof(Ask)];

    protected override void OnInitialized() => _approach =
            SessionStorage.GetItem<Approach?>(StorageKeys.ClientApproach) is { } approach
                ? approach
                : Approach.ReadRetrieveRead;

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
            
            var request = new ChatRequest(history.ToArray(), _approach, Overrides);
            var json = JsonSerializer.Serialize(
                request,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));

            using var body = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await ApiClient.PostAsync("api/chat", body);

            if (response.IsSuccessStatusCode)
            {
                var answer = await response.Content.ReadFromJsonAsync<ApproachResponse>();
                if (answer is not null)
                {
                    _questionAndAnswerMap[_userQuestion] = answer;
                    _userQuestion = "";
                }
            }
            else
            {
                _questionAndAnswerMap[_userQuestion] = new ApproachResponse(
                    $"HTTP {(int)response.StatusCode} : {response.ReasonPhrase ?? "☹️ Unknown error..."}",
                    null,
                    Array.Empty<string>(),
                    "Unable to retrieve valid response from the server.");
            }
        }
        finally
        {
            _isReceivingResponse = false;
        }
    }
}
