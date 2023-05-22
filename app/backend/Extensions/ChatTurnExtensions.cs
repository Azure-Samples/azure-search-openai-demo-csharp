// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Extensions;

internal static class ChatTurnExtensions
{
    internal static string GetChatHistoryAsText(
        this ChatTurn[] history, bool includeLastTurn = true, int approximateMaxTokens = 1_000)
    {
        var historyTextResult = string.Empty;
        var skip = includeLastTurn ? 0 : 1;

        foreach (var turn in history.SkipLast(skip).Reverse())
        {
            var historyText = $"user: {turn.User}";

            if (turn.Bot is not null)
            {
                historyText += $"""
                    <|im_start|>assistant
                    {turn.Bot}
                    <|im_end|>
                    """;
            }

            historyTextResult = historyText + historyTextResult;

            if (historyTextResult.Length > approximateMaxTokens * 4)
            {
                return historyTextResult;
            }
        }

        return historyTextResult;
    }
}
