param name string = deployment().name
param location string = resourceGroup().location
param tags object = {}
param skuName string = 'Enterprise_E5'
param databaseName string = 'default'

resource redisEnterprise_resource 'Microsoft.Cache/redisEnterprise@2024-02-01' = {
  location: location
  name: name
  tags: tags
  sku: {
    name: skuName
  }
}

resource redisEnterprise_default 'Microsoft.Cache/redisEnterprise/databases@2024-02-01' = {
  parent: redisEnterprise_resource
  name: databaseName
  properties: {
    clientProtocol: 'Plaintext'
    clusteringPolicy: 'EnterpriseCluster'
    evictionPolicy: 'NoEviction'
    modules: [
      {
        name: 'RediSearch'
      }
    ]
  }
}

output id string = redisEnterprise_resource.id
output endpoint string = '${redisEnterprise_resource.properties.hostName}:10000'
output name string = redisEnterprise_resource.name
output databaseName string = redisEnterprise_default.name
