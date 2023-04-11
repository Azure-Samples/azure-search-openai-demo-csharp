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

    private static Task<IResult> OnPostChatAsync() => throw new NotImplementedException();

    private static async Task<IResult> OnPostAskAsync(
        AskRequest request, RetrieveThenReadApproachService service)
    {
        if (request is { Question.Length: > 0 })
        {
            var reply = await service.ReplyAsync(request.Question);
            return TypedResults.Ok(reply);
        }

        return Results.BadRequest();
    }
}
