// Copyright (c) Microsoft. All rights reserved.

namespace PrepareDocs.Extensions;

internal static class InvocationContextExtensions
{
    internal static T GetArgValue<T>(
        this InvocationContext context,
        Option<T> option)
    {
        if (context.ParseResult.GetValueForOption(option) is T value)
        {
            return value;
        }

        throw new ArgumentException(
            $"Unable to get parsed value for option: {option.Name}");
    }

    internal static T GetArgValue<T>(
        this InvocationContext context,
        Argument<T> arg)
    {
        if (context.ParseResult.GetValueForArgument(arg) is T value)
        {
            return value;
        }

        throw new ArgumentException(
            $"Unable to get parsed value for argument: {arg.Name}");
    }

}
