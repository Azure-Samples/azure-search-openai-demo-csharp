// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Pages;

public sealed partial class Ask
{
    private string _userQuestion = "";
    private string _lastReferenceQuestion = "";
    private bool _isReceivingResponse = false;
    private AskRespone? _askResponse = null;

    private Approach _approach;

    [Inject] public required ISessionStorageService SessionStorage { get; set; }
    [Inject] public required HttpClient ApiClient { get; set; }

    protected override void OnInitialized()
    {
        _approach =
            SessionStorage.GetItem<Approach?>(StorageKeys.ApproachKey) is { } approach
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
                _askResponse = await response.Content.ReadFromJsonAsync<AskRespone>();
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
