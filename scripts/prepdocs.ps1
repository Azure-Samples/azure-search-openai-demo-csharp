Write-Host ""
Write-Host "Loading azd .env file from current environment"
Write-Host ""

foreach ($line in (& azd env get-values)) {
    if ($line -match "([^=]+)=(.*)") {
        $key = $matches[1]
        $value = $matches[2] -replace '^"|"$'
        [Environment]::SetEnvironmentVariable(
            $key, $value, [System.EnvironmentVariableTarget]::User)
    }
}

Write-Host "Environment variables set."
Write-Host 'Running "PrepareDocs.dll"'

Get-Location | Select-Object -ExpandProperty Path

dotnet run --project "app/prepdocs/PrepareDocs/PrepareDocs.csproj" -- `
  './data/*.pdf' `
  --storageaccount $env:AZURE_STORAGE_ACCOUNT `
  --container $env:AZURE_STORAGE_CONTAINER `
  --searchservice $env:AZURE_SEARCH_SERVICE `
  --index $env:AZURE_SEARCH_INDEX `
  --formrecognizerservice $env:AZURE_FORMRECOGNIZER_SERVICE `
  --tenantid $env:AZURE_TENANT_ID `
  -v
