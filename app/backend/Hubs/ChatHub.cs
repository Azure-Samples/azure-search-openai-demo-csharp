using Microsoft.AspNetCore.SignalR;
using System;

namespace MinimalApi.Hubs;

public class ChatHub : Hub
{
    public const string HubUrl = "/chat-hub";
    private readonly ReadRetrieveReadChatService _chatService;

    public ChatHub(ReadRetrieveReadChatService chatService)
    {
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
    }

    public async Task SendChatRequest(ChatRequest request)
    {
        await _chatService.ReplyStreamingAsync(
            request.History,
            request.Overrides,
            Context.ConnectionId);
    }
}