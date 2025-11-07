// Compute Services Module - Azure Functions, App Service Plans
// Creates serverless compute infrastructure following Azure best practices

@description('Resource prefix for naming consistency')
param resourcePrefix string

@description('Azure region for resource deployment')
param location string = resourceGroup().location

@description('Resource tags for governance')
param tags object

@description('Environment name for configuration')
param environmentName string

@description('Key Vault resource ID for secrets')
param keyVaultId string

@description('Application Insights connection string')
@secure()
param applicationInsightsConnectionString string

@description('Storage account name for Azure Functions')
param storageAccountName string

@description('Service Bus namespace name')
param serviceBusNamespaceName string

@description('Event Hub namespace name')
param eventHubNamespaceName string

@description('Subnet ID for VNet integration')
param subnetId string

@description('PostgreSQL connection string from Key Vault')
@secure()
param postgresConnectionString string

// Variables
var functionAppPlanName = '${resourcePrefix}-func-plan'
var ingestionFunctionName = '${resourcePrefix}-func-ingestion'
var searchFunctionName = '${resourcePrefix}-func-search'

// App Service Plan for Functions
resource functionAppPlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: functionAppPlanName
  location: location
  tags: tags
  sku: {
    name: environmentName == 'prod' ? 'EP1' : 'Y1'
    tier: environmentName == 'prod' ? 'ElasticPremium' : 'Dynamic'
    size: environmentName == 'prod' ? 'EP1' : 'Y1'
    family: environmentName == 'prod' ? 'EP' : 'Y'
  }
  properties: {
    reserved: true // Linux
    maximumElasticWorkerCount: environmentName == 'prod' ? 20 : 10
  }
  kind: 'functionapp'
}

// Storage Account (existing)
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

// Key Vault (existing)
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: last(split(keyVaultId, '/'))
}

// Ingestion Function App
resource ingestionFunctionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: ingestionFunctionName
  location: location
  tags: tags
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: functionAppPlan.id
    reserved: true
    httpsOnly: true
    clientAffinityEnabled: false
    virtualNetworkSubnetId: subnetId
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|8.0'
      functionAppScaleLimit: environmentName == 'prod' ? 200 : 10
      minimumElasticInstanceCount: environmentName == 'prod' ? 1 : 0
      use32BitWorkerProcess: false
      ftpsState: 'Disabled'
      alwaysOn: environmentName == 'prod'
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: '${ingestionFunctionName}-content'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsightsConnectionString
        }
        {
          name: 'ConnectionStrings__DefaultConnection'
          value: '@Microsoft.KeyVault(SecretUri=${postgresConnectionString})'
        }
        {
          name: 'KeyVaultUrl'
          value: keyVault.properties.vaultUri
        }
        {
          name: 'ServiceBusNamespace'
          value: serviceBusNamespaceName
        }
        {
          name: 'EventHubNamespace'
          value: eventHubNamespaceName
        }
        {
          name: 'WEBSITE_VNET_ROUTE_ALL'
          value: '1'
        }
        {
          name: 'WEBSITE_DNS_SERVER'
          value: '168.63.129.16'
        }
      ]
      connectionStrings: [
        {
          name: 'DefaultConnection'
          connectionString: '@Microsoft.KeyVault(SecretUri=${postgresConnectionString})'
          type: 'Custom'
        }
      ]
    }
  }
}

// Search Function App
resource searchFunctionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: searchFunctionName
  location: location
  tags: tags
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: functionAppPlan.id
    reserved: true
    httpsOnly: true
    clientAffinityEnabled: false
    virtualNetworkSubnetId: subnetId
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|8.0'
      functionAppScaleLimit: environmentName == 'prod' ? 200 : 10
      minimumElasticInstanceCount: environmentName == 'prod' ? 1 : 0
      use32BitWorkerProcess: false
      ftpsState: 'Disabled'
      alwaysOn: environmentName == 'prod'
      cors: {
        allowedOrigins: [
          '*'
        ]
        supportCredentials: false
      }
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: '${searchFunctionName}-content'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsightsConnectionString
        }
        {
          name: 'ConnectionStrings__DefaultConnection'
          value: '@Microsoft.KeyVault(SecretUri=${postgresConnectionString})'
        }
        {
          name: 'KeyVaultUrl'
          value: keyVault.properties.vaultUri
        }
        {
          name: 'WEBSITE_VNET_ROUTE_ALL'
          value: '1'
        }
        {
          name: 'WEBSITE_DNS_SERVER'
          value: '168.63.129.16'
        }
      ]
      connectionStrings: [
        {
          name: 'DefaultConnection'
          connectionString: '@Microsoft.KeyVault(SecretUri=${postgresConnectionString})'
          type: 'Custom'
        }
      ]
    }
  }
}

// Grant Function Apps access to Key Vault
resource functionAppsKeyVaultAccess 'Microsoft.KeyVault/vaults/accessPolicies@2023-07-01' = {
  name: 'add'
  parent: keyVault
  properties: {
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: ingestionFunctionApp.identity.principalId
        permissions: {
          secrets: ['get', 'list']
        }
      }
      {
        tenantId: subscription().tenantId
        objectId: searchFunctionApp.identity.principalId
        permissions: {
          secrets: ['get', 'list']
        }
      }
    ]
  }
}

// Outputs
output functionAppNames array = [
  ingestionFunctionApp.name
  searchFunctionApp.name
]
output ingestionFunctionAppName string = ingestionFunctionApp.name
output searchFunctionAppName string = searchFunctionApp.name
output functionAppPlanName string = functionAppPlan.name
