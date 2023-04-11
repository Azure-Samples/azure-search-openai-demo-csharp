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
                var json = JsonSerializer.Serialize(
                    new ChatPrompt { Prompt = prompt },
                    new JsonSerializerOptions(JsonSerializerDefaults.Web));

                using var body = new StringContent(json, Encoding.UTF8, "application/json");
                using var scope = _provider.CreateScope();

                var client = scope.ServiceProvider.GetRequiredService<HttpClient>();
                var response = await client.PostAsync("api/chat", body);

                if (response.IsSuccessStatusCode)
                {
                    using var stream = await response.Content.ReadAsStreamAsync();
                    using var reader = new StreamReader(stream, Encoding.UTF8);

                    // TODO:
                    // There be dragons! 🐉 Look away to avoid hurting your eyes...
                    // I'm not familiar with a way to receive each complete token from the server.
                    // Instead, we read each character individually, and that gets messy!
                    List<char> raw = new();
                    List<char> buffer = new();
                    BufferSegment segment = new();
                    var isEscapeSequence = false;
                    var (sawStart, sawNullStart, sawNullEnd, sawEnd) = (false, false, false, false);
                    while (reader.EndOfStream is false)
                    {
                        var partialResponse = (char)reader.Read();
                        raw.Add(partialResponse);

                        if (partialResponse is '[' && sawStart is false)
                        {
                            sawStart = true;
                            continue;
                        }
                        if (partialResponse is (char)0x0)
                        {
                            if (sawNullStart)
                            {
                                sawNullEnd = true;
                            }
                            else if (sawStart)
                            {
                                sawNullStart = true;
                            }

                            continue;
                        }
                        if (partialResponse is ']' && sawNullEnd && sawEnd is false)
                        {
                            sawEnd = true;
                            continue;
                        }

                        if (partialResponse is '\\')
                        {
                            isEscapeSequence = true;
                            continue;
                        }

                        if (partialResponse is '"')
                        {
                            if (isEscapeSequence)
                            {
                                isEscapeSequence = false;
                            }
                            else
                            {
                                segment = segment is { SawStartingQuote: true }
                                    ? segment with { SawEndingQuote = true }
                                    : segment with { SawStartingQuote = true };

                                continue;
                            }
                        }

                        if (isEscapeSequence)
                        {
                            isEscapeSequence = false;

                            buffer.Add('\\');
                            buffer.Add(partialResponse);

                            continue;
                        }

                        if (partialResponse is ',' &&
                            segment is
                            {
                                SawStartingQuote: true,
                                SawEndingQuote: true
                            })
                        {
                            var bufferedResponse = new string(buffer.ToArray());
                            _responseBuffer.Append(bufferedResponse);
                            buffer.Clear();

                            continue;
                        }

                        buffer.Add(partialResponse);

                        // Required for the Blazor UI to update...
                        // I also tried Task.Delay(0) and Task.Yield(), but neither works.
                        await Task.Delay(1);

                        var responseText = NormalizeResponseText(_responseBuffer, _logger);
                        await handler(
                            new PromptResponse(
                                prompt, responseText));
                    }

                    Console.WriteLine(new string(raw.ToArray()));
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

file readonly record struct BufferSegment(
    bool SawStartingQuote,
    bool SawEndingQuote,
    bool SawEndingComma);
