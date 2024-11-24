using Microsoft.AspNetCore.SignalR;

namespace MinimalApi.Hubs;

public class ChatHub : Hub
{
    public const string HubUrl = "/chat-hub";
    private readonly ReadRetrieveReadChatService _chatService;

    public ChatHub(ReadRetrieveReadChatService chatService)
    {
        _chatService = chatService;
    }

    public async Task SendChatRequest(ChatRequest request)
    {
        try
        {
            request.ConnectionId = Context.ConnectionId;
            await _chatService.ReplyStreamingAsync(
                request.History,
                request.Overrides,
                request.ConnectionId);
        }
        catch
        {
            throw;
        }
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}