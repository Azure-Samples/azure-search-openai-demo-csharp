// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Extensions;

internal static class ConfigurationExtensions
{
    internal static string ToCitationBaseUrl(this IConfiguration config)
    {
        var endpoint = config["AZURE_STORAGE_BLOB_ENDPOINT"];
        ArgumentNullException.ThrowIfNullOrEmpty(endpoint);

        var builder = new UriBuilder(endpoint)
        {
            Path = config["AZURE_STORAGE_CONTAINER"]
        };

        return builder.Uri.AbsoluteUri;
    }
}
