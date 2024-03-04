namespace Shared.Models.Settings;

public sealed class AppSettings
{
    // The KeyVault with all the secrets
    public required string AZURE_KEY_VAULT_ENDPOINT { get; set; }

    public bool UseAOAI { get; set; }
    // Open AI
    public string? OpenAIApiKey { get; set; }
    public string? OpenAiChatGptDeployment { get; set; }
    public string? OpenAiEmbeddingDeployment { get; set; }
    // Azure Open AI
    public string? AzureOpenAiChatGptDeployment { get; set; }
    public string? AzureOpenAiEmbeddingDeployment { get; set; }
    public string? AzureOpenAiServiceEndpoint { get; set; }
    // Azure Open AI GPT4 with Vision
    public bool UseGPT4V { get; set; }
    public string? AzureComputerVisionServiceEndpoint { get; set; }
    public string? AzureComputerVisionServiceApiVersion { get; set; }

    public required string AzureStorageAccountEndpoint { get; set; }
    public required string AzureStorageContainer { get; set; }

    public required string AzureSearchServiceEndpoint { get; set; }
    public required string AzureSearchIndex { get; set; }
}
