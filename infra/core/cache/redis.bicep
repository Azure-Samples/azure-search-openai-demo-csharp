param name string
param location string = resourceGroup().location
param tags object = {}
param keyVaultName string = ''

@description('The pricing tier of the new Azure Redis Cache.')
@allowed([ 'Basic', 'Standard' ])
param cacheSkuName string = 'Basic'

@description('The family for the sku.')
@allowed([ 'C' ])
param cacheSkuFamily string = 'C'

@description('The size of the new Azure Redis Cache instance. ')
@minValue(0)
@maxValue(6)
param cacheSkuCapacity int = 0

@description('Specify a boolean value that indicates whether to allow access via non-SSL ports.')
param enableNonSslPort bool = false

resource redis 'Microsoft.Cache/Redis@2021-06-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    enableNonSslPort: enableNonSslPort
    sku: {
      name: cacheSkuName
      family: cacheSkuFamily
      capacity: cacheSkuCapacity
    }
  }
}

module redisNameSecret '../security/keyvault-secret.bicep' = if (keyVaultName != '') {
  name: 'redis-cache-name'
  params: {
    keyVaultName: keyVaultName
    name: 'RedisCache__Name'
    secretValue: redis.name
  }
}

module redisPrimaryKeySecret '../security/keyvault-secret.bicep' = if (keyVaultName != '') {
  name: 'redis-cache-primary-key'
  params: {
    keyVaultName: keyVaultName
    name: 'RedisCache__PrimaryKey'
    secretValue: redis.properties.accessKeys.primaryKey
  }
}
