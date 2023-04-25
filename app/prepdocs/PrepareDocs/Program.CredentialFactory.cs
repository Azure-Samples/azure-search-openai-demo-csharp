// Copyright (c) Microsoft. All rights reserved.

using Azure.Storage;

internal static partial class Program
{
    internal static AzureKeyCredential GetSearchCredential(AppOptions options)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(options.SearchKey);

        return new AzureKeyCredential(options.SearchKey);
    }

    internal static StorageSharedKeyCredential GetStorageCredential(AppOptions options)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(options.StorageAccount);
        ArgumentNullException.ThrowIfNullOrEmpty(options.StorageKey);

        return new StorageSharedKeyCredential(
            options.StorageAccount, options.StorageKey);
    }
}
