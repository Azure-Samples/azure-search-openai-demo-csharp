param name string
param location string = resourceGroup().location
param tags object = {}

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

resource cache 'Microsoft.Cache/Redis@2021-06-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    sku: {
      name: cacheSkuName
      family: cacheSkuFamily
      capacity: cacheSkuCapacity
    }
  }
}
