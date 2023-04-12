// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Extensions;

internal static class WebApplicationExtensions
{
    internal static WebApplication MapApi(this WebApplication app)
    {
        var api = app.MapGroup("api");

        api.MapGet("content/{citation}", OnGetCitationAsync);
        api.MapPost("chat", OnPostChatAsync);
        api.MapPost("openai/chat", OnPostChatPromptAsync);
        api.MapPost("ask", OnPostAskAsync);

        return app;
    }

    private static async Task<IResult> OnGetCitationAsync(HttpContext http, string citation, BlobContainerClient client)
    {
        if (await client.ExistsAsync() is { Value: false })
        {
            return Results.NotFound("blob container not found");
        }

        var contentDispositionHeader = new ContentDispositionHeaderValue("inline")
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
                        You're name is 'Blazor Clippy'.
                        You will always reply with a Markdown formatted response.
                        """),
                    new ChatMessage(ChatRole.User, "What's your name?"),
                    new ChatMessage(ChatRole.Assistant,
                        "Hi, my name is **Blazor Clippy**! Nice to meet you."),

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
    
    private static async Task<IResult> OnPostChatAsync(ChatRequest request, IServiceProvider sp)
    {
        if (request is { History.Length: > 0 })
        {
            if (request.Approach is Approach.RetrieveThenRead)
            {
                var service = sp.GetRequiredService<RetrieveThenReadApproachService>();
                var question = request.History[^1].User;
                var response = await service.ReplyAsync(question);
                return TypedResults.Ok(response);
            }
            else if (request.Approach is Approach.ReadRetrieveRead)
            {
                var service = sp.GetRequiredService<ReadRetrieveReadChatService>();
                var response = await service.ReplyAsync(request.History, request.Overrides); ;
                return TypedResults.Ok(response);
            }           
        }

        return Results.BadRequest();
    }

    private static async Task<IResult> OnPostAskAsync(
        AskRequest request, RetrieveThenReadApproachService rtr, ReadRetrieveReadApproachService rrr, ReadDecomposeAskApproachService rda)
    {
        if (request is { Question.Length: > 0 })
        {
            if (request.Approach == "rrr")
            {
                var rrrReply = await rrr.ReplyAsync(request.Question, request.Overrides);
                return TypedResults.Ok(rrrReply);
            }
            else if (request.Approach == "rtr")
            {
                var reply = await rtr.ReplyAsync(request.Question);
                return TypedResults.Ok(reply);
            }
            else if (request.Approach == "rda")
            {
                var reply = await rda.ReplyAsync(request.Question, request.Overrides);
                return TypedResults.Ok(reply);
            }
        }

        return Results.BadRequest();
    }
}
