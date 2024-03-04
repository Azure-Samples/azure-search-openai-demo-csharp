namespace Shared.Models;

public record class ChatChunkResponse(
    int Length,
    string Text);
