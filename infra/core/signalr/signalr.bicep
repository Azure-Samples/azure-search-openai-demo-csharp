metadata description = 'Creates an Azure SignalR Services instance.'
param name string
param location string = resourceGroup().location
param tags object = {}

@description('Controls whether local authentication is disabled')
param disableLocalAuth bool = true

@description('The SKU of the SignalR service')
param sku object = {
  name: 'Premium_P1'
}

resource signalR 'Microsoft.SignalRService/signalR@2023-08-01-preview' = {
  name: name
  location: location
  tags: tags
  sku: sku
  properties: {
    disableLocalAuth: disableLocalAuth
  }
}

output endpoint string = 'https://${signalR.properties.hostName}'
output id string = signalR.id
output name string = signalR.name
