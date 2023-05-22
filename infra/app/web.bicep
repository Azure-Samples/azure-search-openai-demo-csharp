param name string
param location string = resourceGroup().location
param tags object = {}

param identityName string
param applicationInsightsName string
param containerAppsEnvironmentName string
param containerRegistryName string
param serviceName string = 'web'
param imageName string = ''
param exists bool
param keyVaultName string
param keyVaultResourceGroupName string = resourceGroup().name
param storageBlobEndpoint string
param storageContainerName string
param searchServiceEndpoint string
param searchIndexName string
param formRecognizerEndpoint string
param openAiEndpoint string
param openAiGptDeployment string
param openAiChatGptDeployment string

param serviceBinds array

resource webIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: identityName
  location: location
}

module webKeyVaultAccess '../core/security/keyvault-access.bicep' = {
  name: 'web-keyvault-access'
  scope: resourceGroup(keyVaultResourceGroupName)
  params: {
    principalId: webIdentity.properties.principalId
    keyVaultName: keyVault.name
  }
}

module app '../core/host/container-app-upsert.bicep' = {
  name: '${serviceName}-container-app'
  dependsOn: [webKeyVaultAccess]
  params: {
    name: name
    location: location
    tags: union(tags, { 'azd-service-name': serviceName })
    identityName: webIdentity.name
    imageName: imageName
    exists: exists
    serviceBinds: serviceBinds
    containerAppsEnvironmentName: containerAppsEnvironmentName
    containerRegistryName: containerRegistryName
    env: [
      {
        name: 'AZURE_CLIENT_ID'
        value: webIdentity.properties.clientId
      }
      {
        name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
        value: applicationInsights.properties.ConnectionString
      }
      {
        name: 'AZURE_KEY_VAULT_ENDPOINT'
        value: keyVault.properties.vaultUri
      }
      {
        name: 'AZURE_STORAGE_BLOB_ENDPOINT'
        value: storageBlobEndpoint
      }
      {
        name: 'AZURE_STORAGE_CONTAINER'
        value: storageContainerName
      }
      {
        name: 'AZURE_SEARCH_SERVICE_ENDPOINT'
        value: searchServiceEndpoint
      }
      {
        name: 'AZURE_SEARCH_INDEX'
        value: searchIndexName
      }
      {
        name: 'AZURE_FORMRECOGNIZER_SERVICE_ENDPOINT'
        value: formRecognizerEndpoint
      }
      {
        name: 'AZURE_OPENAI_ENDPOINT'
        value: openAiEndpoint
      }
      {
        name: 'AZURE_OPENAI_GPT_DEPLOYMENT'
        value: openAiGptDeployment
      }
      {
        name: 'AZURE_OPENAI_CHATGPT_DEPLOYMENT'
        value: openAiChatGptDeployment
      }
    ]
    targetPort: 80
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: applicationInsightsName
}

resource keyVault 'Microsoft.KeyVault/vaults@2022-07-01' existing = {
  name: keyVaultName
  scope: resourceGroup(keyVaultResourceGroupName)
}

output SERVICE_WEB_IDENTITY_PRINCIPAL_ID string = webIdentity.properties.principalId
output SERVICE_WEB_NAME string = app.outputs.name
output SERVICE_WEB_URI string = app.outputs.uri
output SERVICE_WEB_IMAGE_NAME string = app.outputs.imageName
output SERVICE_WEB_IDENTITY_NAME string = identityName
