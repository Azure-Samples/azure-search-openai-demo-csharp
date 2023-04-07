// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Nodes;
using Azure.Storage.Blobs;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("/")]
public class ApiController : Controller
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly BlobContainerClient _blobContainerClient;
    private readonly RetrieveThenReadApproachService _retrieveThenReadApproachService;

    public ApiController(
        BlobServiceClient blobServiceClient, 
        BlobContainerClient blobContainerClient, 
        RetrieveThenReadApproachService retrieveThenReadApproachService)
    {
        this._blobServiceClient = blobServiceClient;
        this._blobContainerClient = blobContainerClient;
        this._retrieveThenReadApproachService = retrieveThenReadApproachService;
    }

    [HttpGet]
    [Route("content/{citation}")]
    public async Task<IActionResult> GetContentAsync(string citation)
    {
        // find out if citation exists in this.blobContainerClient
        // if it does, return the content
        // if it doesn't, return 404
        if (!await this._blobContainerClient.ExistsAsync())
        {
            return this.NotFound("blob container not found");
        }

        var fileContent = await this._blobContainerClient.GetBlobClient(citation).DownloadContentAsync();

        if (fileContent == null)
        {
            return this.NotFound($"{citation} not found");
        }

        return this.Ok(fileContent);
    }

    [HttpPost]
    [Route("chat")]
    [Produces("application/json")]
    public Task<IActionResult> PostChatAsync()
    {
        throw new NotImplementedException();
    }

    [HttpPost]
    [Route("ask")]
    [Produces("application/json")]
    public async Task<IActionResult> PostAskAsync()
    {
        // get question from body
        using var s = new StreamReader(this.Request.Body);
        var json = await s.ReadToEndAsync();
        var doc = JsonNode.Parse(json);
        if (doc!["question"]?.GetValue<string>() is string question)
        {
            var reply = await this._retrieveThenReadApproachService.ReplyAsync(question);

            return this.Ok(reply);
        }

        return this.BadRequest();
    }
}
