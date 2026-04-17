@description('Location for all resources')
param location string = resourceGroup().location

@description('Name of the App Service plan')
param appServicePlanName string = 'weatherapi-plan'

@description('Name of the App Service')
param appServiceName string = 'weatherapi-app'

@description('Name of the PostgreSQL server')
param postgresServerName string = 'weatherapi-db'

@description('PostgreSQL admin username')
param postgresAdminUser string = 'weatheradmin'

@description('PostgreSQL admin password')
@secure()
param postgresAdminPassword string

@description('Name of the App Insights instance')
param appInsightsName string = 'weatherapi-insights'

@description('SKU for App Service plan (F1 for free tier)')
param appServiceSku string = 'F1'

@description('PostgreSQL SKU')
param postgresSku string = 'Basic_B1ms'

var postgresDbName = 'weatherdb'

// App Service Plan
resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: appServiceSku
    tier: 'Free'
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

// Application Insights
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
  }
}

// PostgreSQL Flexible Server
resource postgresServer 'Microsoft.DBforPostgreSQL/flexibleServers@2023-03-01-preview' = {
  name: postgresServerName
  location: location
  sku: {
    name: postgresSku
    tier: 'Basic'
  }
  properties: {
    administratorLogin: postgresAdminUser
    administratorLoginPassword: postgresAdminPassword
    storage: {
      storageSizeGB: 32
    }
    version: '15'
    availabilityZone: '1'
  }
}

// PostgreSQL Database
resource postgresDb 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-03-01-preview' = {
  parent: postgresServer
  name: postgresDbName
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

// Firewall rule to allow Azure services
resource firewallRule 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2023-03-01-preview' = {
  parent: postgresServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// App Service
resource appService 'Microsoft.Web/sites@2023-01-01' = {
  name: appServiceName
  location: location
  kind: 'app,linux'
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: false
      http20Enabled: true
      appSettings: [
        {
          name: 'ConnectionStrings__DefaultConnection'
          value: 'Host=${postgresServer.name}.postgres.database.azure.com;Database=${postgresDbName};Username=${postgresAdminUser}@${postgresServer.name};Password=${postgresAdminPassword};Ssl Mode=Require;'
        }
        {
          name: 'OpenWeatherMap__ApiKey'
          value: '' // Set via deployment settings
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
      ]
    }
  }
}

// Output the App Service URL
output appServiceUrl string = 'https://${appService.properties.defaultHostName}'
