// Copyright (c) Microsoft. All rights reserved.

namespace Backend.Services;

public static class Utils
{
    public static string GetChatHistoryAsText(ChatTurn[] historys, bool includeLastTurn = true, int approxMaxTokens = 1000)
    {
        var res = string.Empty;
        var skip = includeLastTurn ? 0 : 1;
        foreach (var turn in historys.SkipLast(skip).Reverse())
        {
            if (turn.Bot is not null)
            {
                var history = $@"<|im_start|>user
{turn.User}
<|im_end|>
<|im_start|>assistant
{turn.Bot}
<|im_end|>
";
                res = history + res;

                if(res.Length > approxMaxTokens * 4)
                {
                    return res;
                }
            }
        }

        return res;

    }
}
