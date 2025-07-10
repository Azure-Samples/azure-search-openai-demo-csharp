param tags object = {}
param keyVaultName string
param secrets array = []

resource keyVault 'Microsoft.KeyVault/vaults@2022-07-01' existing = {
  name: keyVaultName
}

@batchSize(1)
resource keyVaultSecret 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = [for secret in secrets: {
  parent: keyVault
  name: secret.name
  tags: tags
  properties: {
    attributes: {
      enabled: secret.?enabled ?? true
      exp: secret.?exp ?? 0
      nbf: secret.?nbf ?? 0
    }
    contentType: secret.?contentType ?? 'string'
    value: secret.value
  }
}]
