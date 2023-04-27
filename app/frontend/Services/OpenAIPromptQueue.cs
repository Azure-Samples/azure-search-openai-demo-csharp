// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Services;

public sealed class OpenAIPromptQueue
{
    private readonly IServiceProvider _provider;
    private readonly ILogger<OpenAIPromptQueue> _logger;
    private readonly StringBuilder _responseBuffer = new();
    private Task? _processPromptTask = null;

    public OpenAIPromptQueue(IServiceProvider provider, ILogger<OpenAIPromptQueue> logger) =>
        (_provider, _logger) = (provider, logger);

    public void Enqueue(string prompt, Func<PromptResponse, Task> handler)
    {
        if (_processPromptTask is not null)
        {
            return;
        }

        _processPromptTask = Task.Run(async () =>
        {
            try
            {
                var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
                var json = JsonSerializer.Serialize(
                    new ChatPromptRequest { Prompt = prompt }, options);

                using var body = new StringContent(json, Encoding.UTF8, "application/json");
                using var scope = _provider.CreateScope();

                var factory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
                using var client = factory.CreateClient(typeof(ApiClient).Name);
                var response = await client.PostAsync("api/openai/chat", body);

                if (response.IsSuccessStatusCode)
                {
                    using var stream = await response.Content.ReadAsStreamAsync();

                    await foreach (var chunk in
                        JsonSerializer.DeserializeAsyncEnumerable<ChatChunkResponse>(stream, options))
                    {
                        if (chunk is null)
                        {
                            continue;
                        }

                        _responseBuffer.Append(chunk.Text);

                        var responseText = NormalizeResponseText(_responseBuffer, _logger);
                        await handler(
                            new PromptResponse(
                                prompt, responseText));

                        await Task.Delay(1);
                    }
                }
            }
            catch (Exception ex)
            {
                await handler(
                    new PromptResponse(prompt, ex.Message, true));
            }
            finally
            {
                if (_responseBuffer.Length > 0)
                {
                    var responseText = NormalizeResponseText(_responseBuffer, _logger);
                    await handler(
                        new PromptResponse(
                            prompt, responseText, true));
                    _responseBuffer.Clear();
                }

                _processPromptTask = null;
            }
        });
    }

    private static string NormalizeResponseText(StringBuilder builder, ILogger logger)
    {
        if (builder is null or { Length: 0 })
        {
            return "";
        }

        var text = builder.ToString();

        logger.LogDebug("Before normalize\n\t{Text}", text);

        text = text.StartsWith("null,") ? text[5..] : text;
        text = text.Replace("\r", "\n")
            .Replace("\\n\\r", "\n")
            .Replace("\\n", "\n");

        text = Regex.Unescape(text);

        logger.LogDebug("After normalize:\n\t{Text}", text);

        return text;
    }
}
