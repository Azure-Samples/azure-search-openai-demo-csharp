param logAnalyticsName string
param includeApplicationInsights bool = false
param applicationInsightsName string
param applicationInsightsDashboardName string
param location string = resourceGroup().location
param tags object = {}
param includeDashboard bool = true

module logAnalytics 'loganalytics.bicep' = {
  name: 'loganalytics'
  params: {
    name: logAnalyticsName
    location: location
    tags: tags
  }
}

module applicationInsights 'applicationinsights.bicep' = if (includeApplicationInsights) {
  name: 'applicationinsights'
  params: {
    name: applicationInsightsName
    location: location
    tags: tags
    dashboardName: applicationInsightsDashboardName
    includeDashboard: includeDashboard
    logAnalyticsWorkspaceId: logAnalytics.outputs.id
  }
}

output applicationInsightsConnectionString string = includeApplicationInsights ? applicationInsights.outputs.connectionString : ''
output applicationInsightsInstrumentationKey string = includeApplicationInsights ? applicationInsights.outputs.instrumentationKey : ''
output applicationInsightsName string = includeApplicationInsights ? applicationInsights.outputs.name : ''
output logAnalyticsWorkspaceId string = logAnalytics.outputs.id
output logAnalyticsWorkspaceName string = logAnalytics.outputs.name
