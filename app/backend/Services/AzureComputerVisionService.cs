// Copyright (c) Microsoft. All rights reserved.

using System.Net.Http.Headers;
using System.Text;

namespace MinimalApi.Services;

public class AzureComputerVisionService(IHttpClientFactory httpClientFactory, string endPoint, string apiKey)
{
    // add virtual keyword to make it mockable
    public virtual async Task<ImageEmbeddingResponse> VectorizeImageAsync(string imagePathOrUrl, CancellationToken ct = default)
    {
        var api = $"{endPoint}/computervision/retrieval:vectorizeImage?api-version=2023-02-01-preview&modelVersion=latest";
        // first try to read as local file
        if (File.Exists(imagePathOrUrl))
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, api);

            // set authorization header
            request.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);

            // set body
            var bytes = await File.ReadAllBytesAsync(imagePathOrUrl, ct);
            request.Content = new ByteArrayContent(bytes);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("image/*");

            // send request
            using var client = httpClientFactory.CreateClient();
            using var response = await client.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            // read response
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ImageEmbeddingResponse>(json);

            return result ?? throw new InvalidOperationException("Failed to deserialize response");
        }
        else
        {
            // retrieve as url
            using var request = new HttpRequestMessage(HttpMethod.Post, api);

            // set content type to application/json
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // set authorization header
            request.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);

            // set body
            var body = new { url = imagePathOrUrl };
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            // send request
            using var client = httpClientFactory.CreateClient();
            using var response = await client.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            // read response
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ImageEmbeddingResponse>(json);

            return result ?? throw new InvalidOperationException("Failed to deserialize response");
        }
    }

    public virtual async Task<ImageEmbeddingResponse> VectorizeTextAsync(string text, CancellationToken ct = default)
    {
        var api = $"{endPoint}/computervision/retrieval:vectorizeText?api-version=2023-02-01-preview&modelVersion=latest";

        using var request = new HttpRequestMessage(HttpMethod.Post, api);

        // set content type to application/json
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // set authorization header
        request.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);

        // set body
        var body = new { text };
        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        // send request
        using var client = new HttpClient();
        using var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        // read response
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ImageEmbeddingResponse>(json);
        return result ?? throw new InvalidOperationException("Failed to deserialize response");
    }
}
