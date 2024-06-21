// Copyright (c) Microsoft. All rights reserved.

using System.Net.Http.Headers;

namespace SharedWebComponents.Services;

public sealed class ApiClient(HttpClient httpClient)
{
    public async Task<ImageResponse?> RequestImageAsync(PromptRequest request)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/images", request, SerializerOptions.Default);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ImageResponse>();
    }

    public async Task<bool> ShowLogoutButtonAsync()
    {
        var response = await httpClient.GetAsync("api/enableLogout");
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<bool>();
    }

    public async Task<UploadDocumentsResponse> UploadDocumentsAsync(
        IReadOnlyList<IBrowserFile> files,
        long maxAllowedSize,
        string cookie)
    {
        try
        {
            using var content = new MultipartFormDataContent();

            foreach (var file in files)
            {
                // max allow size: 10mb
                var max_size = maxAllowedSize * 1024 * 1024;
#pragma warning disable CA2000 // Dispose objects before losing scope
                var fileContent = new StreamContent(file.OpenReadStream(max_size));
#pragma warning restore CA2000 // Dispose objects before losing scope
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

                content.Add(fileContent, file.Name, file.Name);
            }

            // set cookie
            content.Headers.Add("X-CSRF-TOKEN-FORM", cookie);
            content.Headers.Add("X-CSRF-TOKEN-HEADER", cookie);

            var response = await httpClient.PostAsync("api/documents", content);

            response.EnsureSuccessStatusCode();

            var result =
                await response.Content.ReadFromJsonAsync<UploadDocumentsResponse>();

            return result
                ?? UploadDocumentsResponse.FromError(
                    "Unable to upload files, unknown error.");
        }
        catch (Exception ex)
        {
            return UploadDocumentsResponse.FromError(ex.ToString());
        }
    }

    public async IAsyncEnumerable<DocumentResponse> GetDocumentsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync("api/documents", cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var options = SerializerOptions.Default;

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

            await foreach (var document in
                JsonSerializer.DeserializeAsyncEnumerable<DocumentResponse>(stream, options, cancellationToken))
            {
                if (document is null)
                {
                    continue;
                }

                yield return document;
            }
        }
    }

    public Task<AnswerResult<ChatRequest>> ChatConversationAsync(ChatRequest request) => PostRequestAsync(request, "api/chat");

    private async Task<AnswerResult<TRequest>> PostRequestAsync<TRequest>(
        TRequest request, string apiRoute) where TRequest : ApproachRequest
    {
        var result = new AnswerResult<TRequest>(
            IsSuccessful: false,
            Response: null,
            Approach: request.Approach,
            Request: request);

        var json = JsonSerializer.Serialize(
            request,
            SerializerOptions.Default);

        using var body = new StringContent(
            json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync(apiRoute, body);

        if (response.IsSuccessStatusCode)
        {
            var answer = await response.Content.ReadFromJsonAsync<ChatAppResponseOrError>();
            return result with
            {
                IsSuccessful = answer is not null,
                Response = answer,
            };
        }
        else
        {
            var errorTitle = $"HTTP {(int)response.StatusCode} : {response.ReasonPhrase ?? "☹️ Unknown error..."}";
            var answer = new ChatAppResponseOrError(
                Array.Empty<ResponseChoice>(),
                errorTitle);

            return result with
            {
                IsSuccessful = false,
                Response = answer
            };
        }
    }
}
