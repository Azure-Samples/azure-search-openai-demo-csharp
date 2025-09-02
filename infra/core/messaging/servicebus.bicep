@description('The name of the Service Bus')
param name string

@description('The location of Service Bus')
param location string = resourceGroup().location

@description('The tags for the Service Bus')
param tags object = {}

@description('The SKU name for the Service Bus')
@allowed(['Basic', 'Standard', 'Premium'])
param skuName string = 'Standard'

@description('Principal ID for MI access')
param principalId string

@description('The queue name')
param queueName string = 'document-processing'

resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2025-05-01-preview' = {
	name: name
	location: location
	tags: tags
	sku:{
		name: skuName
		tier: skuName
	}
	properties: {
		minimumTlsVersion: '1.2'
		publicNetworkAccess: 'Enabled'
		disableLocalAuth: false
		zoneRedundant: skuName == 'Premium'
	}
}

resource documentProcessingQueue 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
  parent: serviceBusNamespace
  name: queueName
  properties: {
    maxSizeInMegabytes: 1024
    maxDeliveryCount: 3
    defaultMessageTimeToLive: 'P14D'
    deadLetteringOnMessageExpiration: true
    requiresDuplicateDetection: false
    enablePartitioning: false
    lockDuration: 'PT5M'
    enableBatchedOperations: true
  }
}

resource serviceBusDataOwnerRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: serviceBusNamespace
  name: guid(serviceBusNamespace.id, principalId, '090c5cfd-751d-490a-894a-3ce6f1109419')
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419') 
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}

output serviceBusEndpoint string = serviceBusNamespace.properties.serviceBusEndpoint
output serviceBusNamespaceName string = serviceBusNamespace.name
output documentProcessingQueueName string = documentProcessingQueue.name