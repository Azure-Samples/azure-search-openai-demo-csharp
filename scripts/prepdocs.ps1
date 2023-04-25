Write-Host ""
Write-Host "Loading azd .env file from current environment"
Write-Host ""

$output = azd env get-values

foreach ($line in $output) {
  $name, $value = $line.Split("=")
  $value = $value -replace '^\"|\"$'
  [Environment]::SetEnvironmentVariable($name, $value)
}

Write-Host "Environment variables set."
Write-Host 'Running "PrepareDocs.dll"'

$cwd = (Get-Location)

dotnet run --project "app/prepdocs/PrepareDocs/PrepareDocs.csproj" -- `
  $cwd/data/*.pdf `
  --storageaccount $env:AZURE_STORAGE_ACCOUNT `
  --container $env:AZURE_STORAGE_CONTAINER `
  --searchservice $env:AZURE_SEARCH_SERVICE `
  --index $env:AZURE_SEARCH_INDEX `
  --formrecognizerservice $env:AZURE_FORMRECOGNIZER_SERVICE `
  --tenantid $env:AZURE_TENANT_ID `
  -v
