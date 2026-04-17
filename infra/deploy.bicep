@description('Resource group name')
param resourceGroupName string = 'rg-weatherapi'

@description('Azure region')
param location string = resourceGroup().location

targetScope = 'subscription'

// Resource Group
resource rg 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: resourceGroupName
  location: location
  tags: {
    project: 'weather-api'
    environment: 'production'
  }
}

// Deploy all resources into the resource group
module resources './main.bicep' = {
  name: 'weather-resources'
  scope: rg
  params: {
    location: location
    appServicePlanName: 'weatherapi-plan'
    appServiceName: 'weatherapi-app'
    postgresServerName: 'weatherapi-db'
    appInsightsName: 'weatherapi-insights'
    postgresAdminUser: 'weatheradmin'
    postgresAdminPassword: '' // Must be provided via parameters file or key vault
  }
}
