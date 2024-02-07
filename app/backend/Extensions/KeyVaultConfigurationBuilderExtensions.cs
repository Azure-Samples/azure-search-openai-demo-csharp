// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Extensions;

internal static class KeyVaultConfigurationBuilderExtensions
{
    internal static ConfigurationManager ConfigureAzureKeyVault(this ConfigurationManager builder)
    {
        var azureKeyVaultEndpoint  = builder["AZURE_KEY_VAULT_ENDPOINT"];
        //var azureKeyVaultEndpoint = Environment.GetEnvironmentVariable("AZURE_KEY_VAULT_ENDPOINT");
        ArgumentNullException.ThrowIfNullOrEmpty(azureKeyVaultEndpoint);

        builder.AddAzureKeyVault(
            new Uri(azureKeyVaultEndpoint), new DefaultAzureCredential());

        return builder;
    }
}
