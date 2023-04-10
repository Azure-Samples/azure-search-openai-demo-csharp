// Copyright (c) Microsoft. All rights reserved.

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

    private static async Task<IResult> OnGetCitationAsync(string citation, BlobContainerClient client)
    {
        if (await client.ExistsAsync() is { Value: false })
        {
            return Results.NotFound("blob container not found");
        }

        var fileContent = await client.GetBlobClient(citation).DownloadContentAsync();
        if (fileContent is null)
        {
            return Results.NotFound($"{citation} not found");
        }

        return TypedResults.Ok(fileContent);
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
