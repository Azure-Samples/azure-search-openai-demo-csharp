param name string
param location string = resourceGroup().location
param tags object = {}

// Microsoft.Web/staticSites/config
param appSettings object={}

param sku object = {
  name: 'Free'
  tier: 'Free'
}

resource web 'Microsoft.Web/staticSites@2022-03-01' = {
  name: name
  location: location
  tags: tags
  sku: sku
  properties: {
    provider: 'Custom'
  }

  resource configAppSettings 'config' = {
    name: 'appsettings'
    properties: appSettings
  }
}

output name string = web.name
output uri string = 'https://${web.properties.defaultHostname}'
