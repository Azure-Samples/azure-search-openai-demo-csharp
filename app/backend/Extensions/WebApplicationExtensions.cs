// Copyright (c) Microsoft. All rights reserved.

using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Backend.Extensions;

internal static class WebApplicationExtensions
{
    internal static WebApplication MapApi(this WebApplication app)
    {
        var api = app.MapGroup("api");

        api.MapGet("content/{citation}", OnGetCitationAsync);
        api.MapPost("chat", OnPostChatAsync);
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

        var contentType = citation.EndsWith(".pdf") ? "application/pdf" : "application/octet-stream";

        http.Response.Headers.ContentDisposition = contentDispositionHeader.ToString();

        return Results.Stream(await client.GetBlobClient(citation).OpenReadAsync(), contentType: contentType);
    }

    private static async Task<IResult> OnPostChatAsync(ChatRequest request, ReadRetrieveReadChatService service)
    {
        if (request is { Approach: "rrr", History.Length: > 0 })
        {
            var response = await service.ReplyAsync(request.History, request.Overrides);
            return TypedResults.Ok(response);
        }

        return Results.BadRequest();
    }

    private static async Task<IResult> OnPostAskAsync(
        AskRequest request, RetrieveThenReadApproachService rtr, ReadRetrieveReadApproachService rrr)
    {
        if (request is { Question.Length: > 0 })
        {
            if (request.Approach == "rrr")
            {
                var rrrReply = await rrr.ReplyAsync(request.Question, request.Overrides);
                return TypedResults.Ok(rrrReply);
            }
            else
            {
                var reply = await rtr.ReplyAsync(request.Question);
                return TypedResults.Ok(reply);
            }
        }

        return Results.BadRequest();
    }
}
