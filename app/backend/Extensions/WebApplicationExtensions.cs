// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Extensions;

internal static class WebApplicationExtensions
{
    internal static WebApplication MapApi(this WebApplication app)
    {
        var api = app.MapGroup("api");

        api.MapGet("content/{citation}", OnGetCitationAsync).CacheOutput();
        api.MapPost("chat", OnPostChatAsync);
        api.MapPost("openai/chat", OnPostChatPromptAsync);
        api.MapPost("ask", OnPostAskAsync);

        return app;
    }

    private static async Task<IResult> OnGetCitationAsync(
        HttpContext http,
        string citation,
        BlobContainerClient client,
        CancellationToken cancellationToken)
    {
        if (await client.ExistsAsync(cancellationToken) is { Value: false })
        {
            return Results.NotFound("blob container not found");
        }

        var contentDispositionHeader =
            new ContentDispositionHeaderValue("inline")
            {
                FileName = citation,
            };

        http.Response.Headers.ContentDisposition = contentDispositionHeader.ToString();
        var contentType = citation.EndsWith(".pdf") ? "application/pdf" : "application/octet-stream";

        return Results.Stream(
            await client.GetBlobClient(citation).OpenReadAsync(cancellationToken: cancellationToken),
            contentType: contentType);
    }

    private static async IAsyncEnumerable<string> OnPostChatPromptAsync(
        ChatPromptRequest prompt,
        OpenAIClient client,
        IConfiguration config,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var deploymentId = config["AZURE_OPENAI_CHATGPT_DEPLOYMENT"];
        var response = await client.GetChatCompletionsStreamingAsync(
            deploymentId, new ChatCompletionsOptions
            {
                Messages =
                {
                    new ChatMessage(ChatRole.System, """
                        You're an AI assistant for developers, helping them write code more efficiently.
                        You're name is **Blazor 📎 Clippy** and you're an expert Blazor developer.
                        You're also an expert in ASP.NET Core, C#, TypeScript, and even JavaScript.
                        You will always reply with a Markdown formatted response.
                        """),

                    new ChatMessage(ChatRole.User, "What's your name?"),

                    new ChatMessage(ChatRole.Assistant,
                        "Hi, my name is **Blazor 📎 Clippy**! Nice to meet you."),

                    new ChatMessage(ChatRole.User, prompt.Prompt)
                }
            }, cancellationToken);

        using var completions = response.Value;
        await foreach (var choice in completions.GetChoicesStreaming(cancellationToken))
        {
            await foreach (var message in choice.GetMessageStreaming(cancellationToken))
            {
                yield return message.Content;
            }
        }
    }

    private static async Task<IResult> OnPostChatAsync(
        ChatRequest request,
        ReadRetrieveReadChatService chatService,
        CancellationToken cancellationToken)
    {
        if (request is { History.Length: > 0 })
        {
            var response = await chatService.ReplyAsync(
                request.History, request.Overrides, cancellationToken);

            return TypedResults.Ok(response);
        }

        return Results.BadRequest();
    }

    private static async Task<IResult> OnPostAskAsync(
        AskRequest request,
        ApproachServiceResponseFactory factory,
        CancellationToken cancellationToken)
    {
        if (request is { Question.Length: > 0 })
        {
            var approachResponse = await factory.GetApproachResponseAsync(
                request.Approach, request.Question, request.Overrides, cancellationToken);

            return TypedResults.Ok(approachResponse);
        }

        return Results.BadRequest();
    }
}
