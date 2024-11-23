using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace MinimalApi.Hubs;

public class ChatHub : Hub
{
    public const string HubUrl = "/chat-hub";
    private readonly ILogger<ChatHub> _logger;
    private readonly ReadRetrieveReadChatService _chatService;

    public ChatHub(ILogger<ChatHub> logger, ReadRetrieveReadChatService chatService)
    {
        _logger = logger;
        _chatService = chatService;
    }

    public async Task SendChatRequest(ChatRequest request)
    {
        try
        {
            request.ConnectionId = Context.ConnectionId;
            await _chatService.ReplyAsync(
                request.History,
                request.Overrides,
                request.ConnectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat request");
            throw;
        }
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation($"Client connected: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation($"Client disconnected: {Context.ConnectionId}");
        if (exception != null)
        {
            _logger.LogError(exception, "Client disconnected with error");
        }
        await base.OnDisconnectedAsync(exception);
    }
}