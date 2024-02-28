// Copyright (c) Microsoft. All rights reserved.

namespace SharedWebComponents.Extensions;

public static class LongExtensions
{
    private static readonly string[] s_sizes = { "B", "KB", "MB", "GB", "TB" };

    public static string ToHumanReadableSize(this long size)
    {
        int order = 0;

        while (size >= 1024 && order < s_sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {s_sizes[order]}";
    }
}
