targetScope = 'resourceGroup'

@description('The location used for all deployed resources')
param location string = resourceGroup().location

@description('Tags that will be applied to all resources')
param tags object = {}

@description('Resource ID of the Azure Container Apps managed environment')
param containerAppsEnvironmentId string

var redisContainerAppName = take('redis-${uniqueString(resourceGroup().id)}', 32)

resource redis 'Microsoft.App/containerApps@2024-02-02-preview' = {
  name: redisContainerAppName
  location: location
  tags: union(tags, {
    'aspire-resource-name': 'redis'
  })
  properties: {
    environmentId: containerAppsEnvironmentId
    configuration: {
      activeRevisionsMode: 'single'
      ingress: {
        external: false
        targetPort: 6379
        transport: 'tcp'
      }
    }
    template: {
      containers: [
        {
          name: 'redis'
          image: 'docker.io/redis:7-alpine'
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
}

output name string = redis.name
output id string = redis.id
output connectionString string = '${redis.name}:6379,abortConnect=false'
