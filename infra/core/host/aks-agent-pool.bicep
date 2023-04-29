param clusterName string

@description('The agent pool name')
param name string

@description('The agent pool configuration')
param config object

resource aksCluster 'Microsoft.ContainerService/managedClusters@2023-01-02-preview' existing = {
  name: clusterName
}

resource nodePool 'Microsoft.ContainerService/managedClusters/agentPools@2023-01-02-preview' = {
  parent: aksCluster
  name: name
  properties: config
}
