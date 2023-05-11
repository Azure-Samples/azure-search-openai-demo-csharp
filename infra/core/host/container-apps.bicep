param name string
param location string = resourceGroup().location
param tags object = {}

param containerAppsEnvironmentName string
param containerRegistryName string
param logAnalyticsWorkspaceName string
param resourceToken string

module containerAppsEnvironment 'container-apps-environment.bicep' = {
  name: '${name}-container-apps-environment'
  params: {
    name: containerAppsEnvironmentName
    location: location
    tags: tags
    logAnalyticsWorkspaceName: logAnalyticsWorkspaceName
  }
}

module containerRegistry 'container-registry.bicep' = {
  name: '${name}-container-registry'
  params: {
    name: containerRegistryName
    location: location
    tags: tags
  }
}

// this launches a redis instance inside of the ACA env
module redis 'springboard-service.bicep' = {
  name: '${name}-dev-service'
  scope: resourceGroup()
  params: {
    name: 'redis-${name}-${resourceToken}'
    location: location
    tags: tags
    managedEnvironmentId: containerAppsEnvironment.outputs.id
    serviceType: 'redis'
  }
}

output defaultDomain string = containerAppsEnvironment.outputs.defaultDomain
output environmentName string = containerAppsEnvironment.outputs.name
output registryLoginServer string = containerRegistry.outputs.loginServer
output registryName string = containerRegistry.outputs.name
output redisServiceBind object = redis.outputs.serviceBind
