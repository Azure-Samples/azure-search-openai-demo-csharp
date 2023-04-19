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
        HttpContext http, string citation, BlobContainerClient client)
    {
        if (await client.ExistsAsync() is { Value: false })
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

        return Results.Stream(await client.GetBlobClient(citation).OpenReadAsync(), contentType: contentType);
    }

    private static async IAsyncEnumerable<string> OnPostChatPromptAsync(
        ChatPromptRequest prompt, OpenAIClient client, IConfiguration config)
    {
        var deploymentId = config["AZURE_OPENAI_CHATGPT_DEPLOYMENT"];
        var response = await client.GetChatCompletionsStreamingAsync(
            deploymentId, new ChatCompletionsOptions
            {
                Messages =
                {
                    new ChatMessage(ChatRole.System, """
                        You're an AI assistant for developers, helping them write code more efficiently.
                        You're name is **Blazor 📎 Clippy**.
                        You're an expert in ASP.NET Core and C#.
                        You will always reply with a Markdown formatted response.
                        """),
                    new ChatMessage(ChatRole.User, "What's your name?"),
                    new ChatMessage(ChatRole.Assistant,
                        "Hi, my name is **Blazor 📎 Clippy**! Nice to meet you."),

                    new ChatMessage(ChatRole.User, prompt.Prompt)
                }
            });

        using var completions = response.Value;
        await foreach (var choice in completions.GetChoicesStreaming())
        {
            await foreach (var message in choice.GetMessageStreaming())
            {
                yield return message.Content;
            }
        }
    }
    
    private static async Task<IResult> OnPostChatAsync(
        ChatRequest request, ReadRetrieveReadChatService chatService)
    {
        if (request is { History.Length: > 0 })
        {
            var response = await chatService.ReplyAsync(
                request.History, request.Overrides);
            
            return TypedResults.Ok(response);
        }

        return Results.BadRequest();
    }

    private static async Task<IResult> OnPostAskAsync(
        AskRequest request, ApproachServiceResponseFactory factory)
    {
        if (request is { Question.Length: > 0 })
        {
            var approachResponse = await factory.GetApproachResponseAsync(
                request.Approach, request.Question, request.Overrides);

            return TypedResults.Ok(approachResponse);
        }

        return Results.BadRequest();
    }
}
