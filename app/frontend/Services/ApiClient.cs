// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Services;

public sealed class ApiClient
{
    private readonly HttpClient _httpClient;

    public ApiClient(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<ApproachResponse?> AskQuestionAsync(AskRequest request)
    {
        var json = JsonSerializer.Serialize(
            request,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        using var body = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("api/ask", body);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<ApproachResponse>();
        }
        else
        {
            return new ApproachResponse(
                $"HTTP {(int)response.StatusCode} : {response.ReasonPhrase ?? "☹️ Unknown error..."}",
                null,
                Array.Empty<string>(),
                "Unable to retrieve valid response from the server.");
        }
    }

    public async Task<AnswerResult<ChatRequest>> ChatConversationAsync(ChatRequest request)
    {
        var result = new AnswerResult<ChatRequest>(
            IsSuccessful: false,
            Response: null,
            Approach: request.Approach,
            Request: request);

        var json = JsonSerializer.Serialize(
            request,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        using var body = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("api/chat", body);

        if (response.IsSuccessStatusCode)
        {
            var answer = await response.Content.ReadFromJsonAsync<ApproachResponse>();
            return result with
            {
                IsSuccessful = answer is not null,
                Response = answer
            };
        }
        else
        {
            var answer = new ApproachResponse(
                $"HTTP {(int)response.StatusCode} : {response.ReasonPhrase ?? "☹️ Unknown error..."}",
                null,
                Array.Empty<string>(),
                "Unable to retrieve valid response from the server.");

            return result with
            {
                IsSuccessful = false,
                Response = answer
            };
        }
    }
}
