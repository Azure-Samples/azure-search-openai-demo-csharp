// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Extensions;

internal static class KeyVaultConfigurationBuilderExtensions
{
    internal static IConfigurationBuilder ConfigureAzureKeyVault(this IConfigurationBuilder builder)
    {
        var azureKeyVaultEndpoint = Environment.GetEnvironmentVariable("AZURE_KEY_VAULT_ENDPOINT") ?? "https://kv-b7mbzbrgs7bfe.vault.azure.net/";
        ArgumentNullException.ThrowIfNullOrEmpty(azureKeyVaultEndpoint);

        builder.AddAzureKeyVault(
            new Uri(azureKeyVaultEndpoint), new DefaultAzureCredential());

        return builder;
    }
}
