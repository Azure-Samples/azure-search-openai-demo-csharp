param name string
param location string = resourceGroup().location
param tags object = {}
@description('The custom subdomain name used to access the API. Defaults to the value of the name parameter.')
param customSubDomainName string = name
param deployments array = []
param kind string = 'OpenAI'
param publicNetworkAccess string = 'Enabled'
param sku object = {
  name: 'S0'
}
param keyVaultName string = ''
param gptDeploymentName string = ''
param chatGptDeploymentName string = ''

resource account 'Microsoft.CognitiveServices/accounts@2022-10-01' = {
  name: name
  location: location
  tags: tags
  kind: kind
  properties: {
    customSubDomainName: customSubDomainName
    publicNetworkAccess: publicNetworkAccess
  }
  sku: sku
}

@batchSize(1)
resource deployment 'Microsoft.CognitiveServices/accounts/deployments@2022-10-01' = [for deployment in deployments: {
  parent: account
  name: deployment.name
  properties: {
    model: deployment.model
    raiPolicyName: contains(deployment, 'raiPolicyName') ? deployment.raiPolicyName : null
    scaleSettings: deployment.scaleSettings
  }
}]

var url = account.properties.endpoint

module openAiServiceEndpointSecret '../security/keyvault-secret.bicep' = if (keyVaultName != '') {
  name: 'openai-service-endpoint-secret'
  params: {
    keyVaultName: keyVaultName
    name: 'AzureOpenAiServiceEndpoint'
    secretValue: url
  }
}

module openAiGptDeploymentSecret '../security/keyvault-secret.bicep' = if (keyVaultName != '') {
  name: 'openai-gpt-deployment-secret'
  params: {
    keyVaultName: keyVaultName
    name: 'AzureOpenAiGptDeployment'
    secretValue: gptDeploymentName
  }
}

module openAiChatGptDeploymentSecret '../security/keyvault-secret.bicep' = if (keyVaultName != '') {
  name: 'openai-chatgpt-deployment-secret'
  params: {
    keyVaultName: keyVaultName
    name: 'AzureOpenAiChatGptDeployment'
    secretValue: chatGptDeploymentName
  }
}

output endpoint string = url
output id string = account.id
output name string = account.name
