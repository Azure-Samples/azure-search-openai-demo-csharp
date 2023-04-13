// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Pages;

public sealed partial class Ask
{
    private string _userQuestion = "";
    private string _lastReferenceQuestion = "";
    private bool _isReceivingResponse = false;
    private ApproachResponse? _approachResponse = null;
    private Approach _approach;

    [Inject] public required ISessionStorageService SessionStorage { get; set; }
    [Inject] public required HttpClient ApiClient { get; set; }
    
    [CascadingParameter] public RequestOverrides? Overrides { get; set; }
    
    protected override void OnInitialized()
    {
        _approach =
            SessionStorage.GetItem<Approach?>(StorageKeys.ClientApproach) is { } approach
                ? approach
                : Approach.RetrieveThenRead;
    }

    private Task OnAskQuestionAsync(string question)
    {
        _userQuestion = question;
        return OnAskClickedAsync();
    }

    private async Task OnAskClickedAsync()
    {
        _isReceivingResponse = true;
        _lastReferenceQuestion = _userQuestion;

        try
        {
            var request = new AskRequest(_userQuestion, _approach, Overrides);
            var json = JsonSerializer.Serialize(
                request,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));

            using var body = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await ApiClient.PostAsync("api/ask", body);

            if (response.IsSuccessStatusCode)
            {
                _approachResponse = await response.Content.ReadFromJsonAsync<ApproachResponse>();
            }
            else
            {
                _approachResponse = new ApproachResponse(
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
