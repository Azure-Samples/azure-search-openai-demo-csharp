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

if ([string]::IsNullOrEmpty($env:AZD_PREPDOCS_RAN) -or $env:AZD_PREPDOCS_RAN -eq "false") {
    Write-Host 'Running "PrepareDocs.dll"'

    Get-Location | Select-Object -ExpandProperty Path

    dotnet run --project "app/prepdocs/PrepareDocs/PrepareDocs.csproj" -- `
        './data/*.pdf' `
        --storageendpoint $env:AZURE_STORAGE_BLOB_ENDPOINT `
        --container $env:AZURE_STORAGE_CONTAINER `
        --searchendpoint $env:AZURE_SEARCH_SERVICE_ENDPOINT `
        --searchindex $env:AZURE_SEARCH_INDEX `
        --formrecognizerendpoint $env:AZURE_FORMRECOGNIZER_SERVICE_ENDPOINT `
        --tenantid $env:AZURE_TENANT_ID `
        -v

    azd env set AZD_PREPDOCS_RAN "true"
} else {
    Write-Host "AZD_PREPDOCS_RAN is set to true. Skipping the run."
}
