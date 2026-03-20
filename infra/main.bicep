// BillingSys: Azure Storage (Table) + Consumption Linux Function App
// Deploy: az deployment group create -g <resource-group> -f infra/main.bicep -p functionAppName=<unique-name> -p storageAccountName=<unique-lower-3-24>

@description('Azure region for all resources')
param location string = resourceGroup().location

@description('Globally unique function app name (e.g. billingsys-func-tech85)')
param functionAppName string

@description('Globally unique storage account name (lowercase letters and numbers only, 3-24 chars)')
param storageAccountName string

var hostingPlanName = '${functionAppName}-plan'

resource storage 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    allowBlobPublicAccess: false
  }
}

resource hostingPlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: hostingPlanName
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  properties: {
    reserved: true
  }
}

resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: hostingPlan.id
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|8.0'
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storage.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storage.listKeys().keys[0].value}'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
      ]
      cors: {
        allowedOrigins: [
          '*'
        ]
        supportCredentials: false
      }
    }
    httpsOnly: true
  }
}

output storageAccountName string = storage.name
output functionAppName string = functionApp.name
output functionAppHostName string = functionApp.properties.defaultHostName
