// Copyright (c) Microsoft. All rights reserved.

using PrepareDocs.Extensions;

internal static partial class Program
{
    internal static DefaultAzureCredential DefaultCredential { get; } = new();

    internal static AzureCliCredential GetTenantIdCredential(InvocationContext context)
    {
        if (context.ParseResult.GetValueForOption(s_tenantId) is string tenantId)
        {
            return new AzureCliCredential(new AzureCliCredentialOptions
            {
                TenantId = tenantId
            });
        }

        return new AzureCliCredential();
    }

    internal static AzureKeyCredential? GetSearchCredential(InvocationContext context)
    {
        if (context.ParseResult.GetValueForOption(s_searchKey) is string searchKey)
        {
            return new AzureKeyCredential(searchKey);
        }

        return null;
    }

    internal static AzureKeyCredential GetStorageCredential(InvocationContext context) =>
        new(context.GetArgValue(s_storageKey));
}
