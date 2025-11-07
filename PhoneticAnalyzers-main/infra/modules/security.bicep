// Security Module - RBAC, Monitoring, Alerts
// Creates security and compliance infrastructure following Azure best practices

@description('Resource prefix for naming consistency')
param resourcePrefix string

@description('Azure region for resource deployment')
param location string = resourceGroup().location

@description('Resource tags for governance')
param tags object

@description('Environment name for configuration')
param environmentName string

@description('Key Vault resource ID')
param keyVaultId string

@description('Log Analytics workspace ID for monitoring')
param logAnalyticsWorkspaceId string

@description('Function app names for monitoring')
param functionAppNames array

@description('PostgreSQL server resource ID')
param postgresServerId string

// Variables
var actionGroupName = '${resourcePrefix}-alerts'

// Action Group for Alerts
resource alertActionGroup 'Microsoft.Insights/actionGroups@2023-01-01' = {
  name: actionGroupName
  location: 'Global'
  tags: tags
  properties: {
    groupShortName: 'PhonAlerts'
    enabled: true
    emailReceivers: [
      {
        name: 'AdminEmail'
        emailAddress: 'admin@company.com'
        useCommonAlertSchema: true
      }
    ]
    smsReceivers: []
    webhookReceivers: []
    armRoleReceivers: []
    azureAppPushReceivers: []
    itsmReceivers: []
    automationRunbookReceivers: []
    voiceReceivers: []
    logicAppReceivers: []
    azureFunctionReceivers: []
    eventHubReceivers: []
  }
}

// Database Connection Alert
resource dbConnectionAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = {
  name: '${resourcePrefix}-db-connection-alert'
  location: 'Global'
  tags: tags
  properties: {
    description: 'Alert when database connections are high'
    severity: 2
    enabled: true
    scopes: [
      postgresServerId
    ]
    evaluationFrequency: 'PT1M'
    windowSize: 'PT5M'
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          name: 'DatabaseConnections'
          metricName: 'active_connections'
          metricNamespace: 'Microsoft.DBforPostgreSQL/flexibleServers'
          operator: 'GreaterThan'
          threshold: environmentName == 'prod' ? 80 : 20
          timeAggregation: 'Average'
          criterionType: 'StaticThresholdCriterion'
        }
      ]
    }
    actions: [
      {
        actionGroupId: alertActionGroup.id
      }
    ]
  }
}

// Function App Error Rate Alert
resource functionErrorRateAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = [for functionName in functionAppNames: {
  name: '${functionName}-error-rate-alert'
  location: 'Global'
  tags: tags
  properties: {
    description: 'Alert when function app error rate is high'
    severity: 1
    enabled: true
    scopes: [
      resourceId('Microsoft.Web/sites', functionName)
    ]
    evaluationFrequency: 'PT1M'
    windowSize: 'PT5M'
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          name: 'ErrorRate'
          metricName: 'FunctionExecutionCount'
          metricNamespace: 'Microsoft.Web/sites'
          operator: 'GreaterThan'
          threshold: 5
          timeAggregation: 'Total'
          criterionType: 'StaticThresholdCriterion'
        }
      ]
    }
    actions: [
      {
        actionGroupId: alertActionGroup.id
      }
    ]
  }
}]

// Key Vault Access Alert (using Activity Log)
resource keyVaultAccessAlert 'Microsoft.Insights/activityLogAlerts@2020-10-01' = {
  name: '${resourcePrefix}-keyvault-access-alert'
  location: 'Global'
  tags: tags
  properties: {
    description: 'Alert on Key Vault access attempts'
    enabled: true
    scopes: [
      keyVaultId
    ]
    condition: {
      allOf: [
        {
          field: 'category'
          equals: 'Security'
        }
        {
          field: 'operationName'
          equals: 'Microsoft.KeyVault/vaults/read'
        }
      ]
    }
    actions: {
      actionGroups: [
        {
          actionGroupId: alertActionGroup.id
        }
      ]
    }
  }
}

// Log Analytics Queries for Security Monitoring
resource securityWorkbook 'Microsoft.Insights/workbooks@2023-06-01' = {
  name: guid('${resourcePrefix}-security-workbook')
  location: location
  tags: tags
  kind: 'shared'
  properties: {
    displayName: '${resourcePrefix} Security Dashboard'
    serializedData: '''
{
  "version": "Notebook/1.0",
  "items": [
    {
      "type": 1,
      "content": {
        "json": "## Security Monitoring Dashboard\\n\\nMonitor security events, failed authentications, and access patterns."
      },
      "name": "text - 0"
    },
    {
      "type": 3,
      "content": {
        "version": "KqlItem/1.0",
        "query": "AzureActivity\\n| where TimeGenerated > ago(24h)\\n| where ActivityStatusValue == \\"Failure\\"\\n| summarize count() by bin(TimeGenerated, 1h), OperationNameValue\\n| render timechart",
        "size": 0,
        "title": "Failed Operations (Last 24h)",
        "queryType": 0,
        "resourceType": "microsoft.operationalinsights/workspaces"
      },
      "name": "query - 1"
    }
  ],
  "isLocked": false
}
'''
    sourceId: logAnalyticsWorkspaceId
    category: 'workbook'
  }
}

// Network Security Group (if needed)
resource networkSecurityGroup 'Microsoft.Network/networkSecurityGroups@2023-11-01' = {
  name: '${resourcePrefix}-nsg'
  location: location
  tags: tags
  properties: {
    securityRules: [
      {
        name: 'AllowHTTPS'
        properties: {
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '443'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
          access: 'Allow'
          priority: 1000
          direction: 'Inbound'
        }
      }
      {
        name: 'AllowPostgreSQL'
        properties: {
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '5432'
          sourceAddressPrefix: 'VirtualNetwork'
          destinationAddressPrefix: '*'
          access: 'Allow'
          priority: 1100
          direction: 'Inbound'
        }
      }
      {
        name: 'DenyAllInbound'
        properties: {
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRange: '*'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
          access: 'Deny'
          priority: 4096
          direction: 'Inbound'
        }
      }
    ]
  }
}

// Outputs
output actionGroupId string = alertActionGroup.id
output networkSecurityGroupId string = networkSecurityGroup.id
output securityWorkbookId string = securityWorkbook.id
