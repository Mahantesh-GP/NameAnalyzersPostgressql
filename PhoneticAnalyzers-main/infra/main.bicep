// Bicep template for Azure Phonetic Analyzers Infrastructure
// This template creates a complete production-ready infrastructure following Azure best practices

targetScope = 'subscription'

// Parameters
@description('The environment name (e.g., dev, staging, prod)')
@allowed(['dev', 'staging', 'prod'])
param environmentName string = 'dev'

@description('The application name')
param appName string = 'phoneticanalyzers'

@description('The location for all resources')
param location string = 'East US 2'

@description('The administrator email for alerts')
param adminEmail string

@description('Enable zone redundancy for high availability')
param enableZoneRedundancy bool = true

@description('Enable geo-redundant backups')
param enableGeoRedundancy bool = true

@description('PostgreSQL administrator username')
@secure()
param postgresAdminUsername string

@description('PostgreSQL administrator password')
@secure()
param postgresAdminPassword string

// Variables
var resourcePrefix = '${appName}-${environmentName}'
var resourceGroupName = 'rg-${resourcePrefix}'
var tags = {
  Environment: environmentName
  Application: appName
  'Cost-Center': 'Engineering'
  CreatedBy: 'Bicep-IaC'
}

// Resource Group
resource resourceGroup 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}

// Deploy core infrastructure
module coreInfrastructure './modules/core-infrastructure.bicep' = {
  name: 'core-infrastructure-deployment'
  scope: resourceGroup
  params: {
    resourcePrefix: resourcePrefix
    location: location
    tags: tags
    environmentName: environmentName
    adminEmail: adminEmail
  }
}

// Deploy data services
module dataServices './modules/data-services.bicep' = {
  name: 'data-services-deployment'
  scope: resourceGroup
  params: {
    resourcePrefix: resourcePrefix
    location: location
    tags: tags
    environmentName: environmentName
    enableZoneRedundancy: enableZoneRedundancy
    enableGeoRedundancy: enableGeoRedundancy
    postgresAdminUsername: postgresAdminUsername
    postgresAdminPassword: postgresAdminPassword
    keyVaultId: coreInfrastructure.outputs.keyVaultId
    logAnalyticsWorkspaceId: coreInfrastructure.outputs.logAnalyticsWorkspaceId
    subnetId: coreInfrastructure.outputs.privateSubnetId
  }
}

// Deploy compute services
module computeServices './modules/compute-services.bicep' = {
  name: 'compute-services-deployment'
  scope: resourceGroup
  params: {
    resourcePrefix: resourcePrefix
    location: location
    tags: tags
    environmentName: environmentName
    keyVaultId: coreInfrastructure.outputs.keyVaultId
    applicationInsightsConnectionString: coreInfrastructure.outputs.applicationInsightsConnectionString
    storageAccountName: coreInfrastructure.outputs.storageAccountName
    serviceBusNamespaceName: dataServices.outputs.serviceBusNamespaceName
    eventHubNamespaceName: dataServices.outputs.eventHubNamespaceName
    subnetId: coreInfrastructure.outputs.privateSubnetId
    postgresConnectionString: dataServices.outputs.postgresConnectionString
  }
}

// Deploy security and compliance
module security './modules/security.bicep' = {
  name: 'security-deployment'
  scope: resourceGroup
  params: {
    resourcePrefix: resourcePrefix
    location: location
    tags: tags
    environmentName: environmentName
    keyVaultId: coreInfrastructure.outputs.keyVaultId
    logAnalyticsWorkspaceId: coreInfrastructure.outputs.logAnalyticsWorkspaceId
    functionAppNames: computeServices.outputs.functionAppNames
    postgresServerId: dataServices.outputs.postgresServerId
  }
}

// Outputs
output resourceGroupName string = resourceGroupName
output keyVaultName string = coreInfrastructure.outputs.keyVaultName
output functionAppNames array = computeServices.outputs.functionAppNames
output postgresServerName string = dataServices.outputs.postgresServerName
output applicationInsightsName string = coreInfrastructure.outputs.applicationInsightsName
output storageAccountName string = coreInfrastructure.outputs.storageAccountName