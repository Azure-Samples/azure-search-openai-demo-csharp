param name string
param location string = resourceGroup().location
param containerAppEnvironmentId string
param tags object = {}
param envVars array = []
param minReplicas int = 1
param maxReplicas int = 1

resource acr 'Microsoft.ContainerRegistry/registries@2022-12-01' = {
  name: toLower('${resourceGroup().name}acr')
  location: location
  tags: tags
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: true
  }
}

resource containerApp 'Microsoft.app/containerApps@2022-10-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    managedEnvironmentId: containerAppEnvironmentId
    configuration: {
      activeRevisionsMode: 'multiple'
      secrets: [
        {
          name: 'container-registry-password'
          value: acr.listCredentials().passwords[0].value
        }
      ]
      registries: [
        {
          server: acr.name
          username: acr.listCredentials().username
          passwordSecretRef: 'container-registry-password'
        }
      ]
      ingress: {
        external: true
        targetPort: 80
        transport: 'http'
        allowInsecure: true
      }
    }
    template: {
      containers: [
        {
          name: 'app'
          image: 'nginx' // TODO: determine if this needs to be repaced.
          env: envVars
        }
      ]
      scale: {
        minReplicas: minReplicas
        maxReplicas: maxReplicas
      }
    }
  }
}

output appUrl string = containerApp.properties.configuration.ingress.fqdn
output identityPrincipalId string = containerApp.identity.principalId
