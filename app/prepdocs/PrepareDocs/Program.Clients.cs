// Copyright (c) Microsoft. All rights reserved.

internal static partial class Program
{
    private static BlobContainerClient? s_containerClient;
    private static readonly SemaphoreSlim s_lock = new(1);

    private static async Task<BlobContainerClient> GetBlobContainerClientAsync(AppOptions options)
    {
        await s_lock.WaitAsync();

        try
        {
            if (s_containerClient is null)
            {
                var blobService = new BlobServiceClient(
                    new Uri($"https://{options.StorageAccount}.blob.core.windows.net"),
                    DefaultCredential);

                s_containerClient = blobService.GetBlobContainerClient(options.Container);
                await s_containerClient.CreateIfNotExistsAsync();
            }

            return s_containerClient;
        }
        finally
        {
            s_lock.Release();
        }
    }
}
