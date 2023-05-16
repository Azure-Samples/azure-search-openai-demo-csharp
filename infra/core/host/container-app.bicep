param name string
param location string = resourceGroup().location
param tags object = {}

param containerAppsEnvironmentName string
param containerName string = 'main'
param containerRegistryName string = ''

@allowed([ 'Single', 'Multiple' ])
param revisionMode string = 'Single'

@description('Minimum number of replicas to run')
@minValue(1)
param containerMinReplicas int = 1
@description('Maximum number of replicas to run')
@minValue(1)
param containerMaxReplicas int = 10

param secrets array = []
param env array = []
param external bool = true
param imageName string
param targetPort int = 80
@allowed([ 'None', 'SystemAssigned', 'UserAssigned' ])
param identityType string = 'None'
param serviceBinds array = []
param allowedOrigins array = []

@description('User assigned identity name')
param identityName string = ''

@description('Enabled Ingress for container app')
param ingressEnabled bool = true

// Dapr Options
@description('Enable Dapr')
param daprEnabled bool = false
@description('Dapr app ID')
param daprAppId string = containerName
@allowed([ 'http', 'grpc' ])
@description('Protocol used by Dapr to connect to the app, e.g. http or grpc')
param daprAppProtocol string = 'http'

@description('CPU cores allocated to a single container instance, e.g. 0.5')
param containerCpuCoreCount string = '0.5'

@description('Memory allocated to a single container instance, e.g. 1Gi')
param containerMemory string = '1.0Gi'

resource userIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = if (!empty(identityName)) {
  name: identityName
}

// Private registry support requires both an ACR name and a User Assigned managed identity
var usePrivateRegistry = !empty(identityName) && !empty(containerRegistryName)

// Automatically set to `UserAssigned` when an `identityName` has been set
var normalizedIdentityType = !empty(identityName) ? 'UserAssigned' : identityType

module containerRegistryAccess '../security/registry-access.bicep' = if (usePrivateRegistry) {
  name: '${deployment().name}-registry-access'
  params: {
    containerRegistryName: containerRegistryName
    principalId: usePrivateRegistry ? userIdentity.properties.principalId : ''
  }
}

resource app 'Microsoft.App/containerApps@2022-11-01-preview' = {
  name: name
  location: location
  tags: tags
  // It is critical that the identity is granted ACR pull access before the app is created
  // otherwise the container app will throw a provision error
  // This also forces us to use an user assigned managed identity since there would no way to 
  // provide the system assigned identity with the ACR pull access before the app is created
  dependsOn: usePrivateRegistry ? [ containerRegistryAccess ] : []
  identity: {
    type: normalizedIdentityType
    userAssignedIdentities: !empty(identityName) && normalizedIdentityType == 'UserAssigned' ? { '${userIdentity.id}': {} } : null
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      activeRevisionsMode: revisionMode
      ingress: ingressEnabled ? {
        external: external
        targetPort: targetPort
        transport: 'auto'
        corsPolicy: {
          allowedOrigins: union([ 'https://portal.azure.com', 'https://ms.portal.azure.com' ], allowedOrigins)
        }
      } : null
      dapr: daprEnabled ? {
        enabled: true
        appId: daprAppId
        appProtocol: daprAppProtocol
        appPort: ingressEnabled ? targetPort : 0
      } : { enabled: false }
      secrets: secrets
      registries: usePrivateRegistry ? [
        {
          server: '${containerRegistryName}.azurecr.io'
          identity: userIdentity.id
        }
      ] : []
    }
    template: {
      serviceBinds: serviceBinds
      containers: [
        {
          image: !empty(imageName) ? imageName : 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
          name: containerName
          env: env
          resources: {
            cpu: json(containerCpuCoreCount)
            memory: containerMemory
          }
        }
      ]
      scale: {
        minReplicas: containerMinReplicas
        maxReplicas: containerMaxReplicas
      }
    }
  }
}

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2022-11-01-preview' existing = {
  name: containerAppsEnvironmentName
}

output defaultDomain string = containerAppsEnvironment.properties.defaultDomain
output imageName string = imageName
output name string = app.name
output uri string = 'https://${app.properties.configuration.ingress.fqdn}'
output identityPrincipalId string = normalizedIdentityType == 'None' ? '' : (empty(identityName) ? app.identity.principalId : userIdentity.properties.principalId)
