// Data Services Module - PostgreSQL, Service Bus, Event Hubs
// Creates messaging and database infrastructure following Azure best practices

@description('Resource prefix for naming consistency')
param resourcePrefix string

@description('Azure region for resource deployment')
param location string = resourceGroup().location

@description('Resource tags for governance')
param tags object

@description('Environment name for configuration')
param environmentName string

@description('Enable zone redundancy for high availability')
param enableZoneRedundancy bool

@description('Enable geo-redundant backups')
param enableGeoRedundancy bool

@description('PostgreSQL administrator username')
@secure()
param postgresAdminUsername string

@description('PostgreSQL administrator password')
@secure()
param postgresAdminPassword string

@description('Key Vault resource ID for secrets')
param keyVaultId string

@description('Log Analytics workspace ID for diagnostics')
param logAnalyticsWorkspaceId string

@description('Subnet ID for private endpoints')
param subnetId string

// Variables
var postgresServerName = '${resourcePrefix}-postgres'
var serviceBusNamespaceName = '${resourcePrefix}-servicebus'
var eventHubNamespaceName = '${resourcePrefix}-eventhub'

// PostgreSQL Flexible Server
resource postgresServer 'Microsoft.DBforPostgreSQL/flexibleServers@2023-12-01-preview' = {
  name: postgresServerName
  location: location
  tags: tags
  sku: {
    name: environmentName == 'prod' ? 'Standard_D4s_v3' : 'Standard_B2s'
    tier: environmentName == 'prod' ? 'GeneralPurpose' : 'Burstable'
  }
  properties: {
    administratorLogin: postgresAdminUsername
    administratorLoginPassword: postgresAdminPassword
    version: '15'
    storage: {
      storageSizeGB: environmentName == 'prod' ? 512 : 128
      autoGrow: 'Enabled'
    }
    backup: {
      backupRetentionDays: environmentName == 'prod' ? 35 : 7
      geoRedundantBackup: enableGeoRedundancy ? 'Enabled' : 'Disabled'
    }
    highAvailability: enableZoneRedundancy ? {
      mode: 'ZoneRedundant'
    } : {
      mode: 'Disabled'
    }
    network: {
      publicNetworkAccess: 'Disabled'
    }
  }
}

// PostgreSQL Database
resource phoneticsDatabase 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-12-01-preview' = {
  name: 'phonetics'
  parent: postgresServer
  properties: {
    charset: 'UTF8'
    collation: 'en_US.UTF8'
  }
}

// PostgreSQL Extensions
resource trigramExtension 'Microsoft.DBforPostgreSQL/flexibleServers/configurations@2023-12-01-preview' = {
  name: 'shared_preload_libraries'
  parent: postgresServer
  properties: {
    value: 'pg_trgm'
    source: 'user-override'
  }
}

// Service Bus Namespace
resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: serviceBusNamespaceName
  location: location
  tags: tags
  sku: {
    name: environmentName == 'prod' ? 'Premium' : 'Standard'
    tier: environmentName == 'prod' ? 'Premium' : 'Standard'
  }
  properties: {
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Disabled'
    disableLocalAuth: true
    zoneRedundant: enableZoneRedundancy
  }
}

// Service Bus Queue for Person Ingestion
resource personIngestionQueue 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  name: 'person-ingestion'
  parent: serviceBusNamespace
  properties: {
    lockDuration: 'PT5M'
    maxSizeInMegabytes: 1024
    requiresDuplicateDetection: false
    requiresSession: false
    defaultMessageTimeToLive: 'P14D'
    deadLetteringOnMessageExpiration: true
    maxDeliveryCount: 10
    duplicateDetectionHistoryTimeWindow: 'PT10M'
    enableBatchedOperations: true
    enablePartitioning: false
  }
}

// Event Hubs Namespace
resource eventHubNamespace 'Microsoft.EventHub/namespaces@2024-01-01' = {
  name: eventHubNamespaceName
  location: location
  tags: tags
  sku: {
    name: environmentName == 'prod' ? 'Standard' : 'Basic'
    tier: environmentName == 'prod' ? 'Standard' : 'Basic'
  }
  properties: {
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Disabled'
    disableLocalAuth: true
    zoneRedundant: enableZoneRedundancy
  }
}

// Event Hub for Analytics Events
resource analyticsEventHub 'Microsoft.EventHub/namespaces/eventhubs@2024-01-01' = {
  name: 'phonetic-analytics'
  parent: eventHubNamespace
  properties: {
    messageRetentionInDays: environmentName == 'prod' ? 7 : 1
    partitionCount: environmentName == 'prod' ? 4 : 2
  }
}

// Private Endpoints
resource postgresPrivateEndpoint 'Microsoft.Network/privateEndpoints@2023-11-01' = {
  name: '${postgresServerName}-pe'
  location: location
  tags: tags
  properties: {
    subnet: {
      id: subnetId
    }
    privateLinkServiceConnections: [
      {
        name: '${postgresServerName}-pe-connection'
        properties: {
          privateLinkServiceId: postgresServer.id
          groupIds: ['postgresqlServer']
        }
      }
    ]
  }
}

resource serviceBusPrivateEndpoint 'Microsoft.Network/privateEndpoints@2023-11-01' = {
  name: '${serviceBusNamespaceName}-pe'
  location: location
  tags: tags
  properties: {
    subnet: {
      id: subnetId
    }
    privateLinkServiceConnections: [
      {
        name: '${serviceBusNamespaceName}-pe-connection'
        properties: {
          privateLinkServiceId: serviceBusNamespace.id
          groupIds: ['namespace']
        }
      }
    ]
  }
}

// Diagnostic Settings
resource postgresDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'postgres-diagnostics'
  scope: postgresServer
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      {
        categoryGroup: 'allLogs'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: environmentName == 'prod' ? 90 : 30
        }
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: environmentName == 'prod' ? 90 : 30
        }
      }
    ]
  }
}

// Store connection string in Key Vault
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: last(split(keyVaultId, '/'))
}

resource postgresConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'postgres-connection-string'
  parent: keyVault
  properties: {
    value: 'Host=${postgresServer.properties.fullyQualifiedDomainName};Database=phonetics;Username=${postgresAdminUsername};Password=${postgresAdminPassword};SSL Mode=Require;Trust Server Certificate=true'
  }
}

// Outputs
output postgresServerId string = postgresServer.id
output postgresServerName string = postgresServer.name
output serviceBusNamespaceName string = serviceBusNamespace.name
output eventHubNamespaceName string = eventHubNamespace.name
output postgresConnectionString string = postgresConnectionStringSecret.properties.secretUri
