if (-not $script:azdCmd) {
    $script:azdCmd = Get-Command azd -ErrorAction SilentlyContinue
}

# Check if 'azd' command is available
if (-not $script:azdCmd) {
    throw "Error: 'azd' command is not found. Please ensure you have 'azd' installed. For installation instructions, visit: https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/install-azd"
}

if (-not $script:dotnetCmd) {
    $script:dotnetCmd = Get-Command dotnet -ErrorAction SilentlyContinue
}

# Check if 'dotnet' command is available
if (-not $script:dotnetCmd) {
    throw "Error: 'dotnet' command is not found. Please ensure you have 'dotnet' installed. For installation instructions, visit: https://learn.microsoft.com/en-us/dotnet/core/tools/"
}

function Invoke-ExternalCommand {
    param (
        [Parameter(Mandatory = $true)]
        [string]$Command,
        
        [Parameter(Mandatory = $false)]
        [string]$Arguments
    )

    $processStartInfo = New-Object System.Diagnostics.ProcessStartInfo
    $processStartInfo.FileName = $Command
    $processStartInfo.Arguments = $Arguments
    $processStartInfo.RedirectStandardOutput = $true
    $processStartInfo.RedirectStandardError = $true
    $processStartInfo.UseShellExecute = $false
    $processStartInfo.CreateNoWindow = $true

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $processStartInfo
    $process.Start() | Out-Null
    $output = $process.StandardOutput.ReadToEnd()
    $errorOutput = $process.StandardError.ReadToEnd()
    $process.WaitForExit()

    if ($errorOutput) {
        Write-Error $errorOutput
    }

    return $output
}

if ([string]::IsNullOrEmpty($env:AZD_PREPDOCS_RAN) -or $env:AZD_PREPDOCS_RAN -eq "false") {
    Write-Host 'Running "PrepareDocs.dll"'

    Get-Location | Select-Object -ExpandProperty Path

    $dotnetArguments = @"
    run --project "app/prepdocs/PrepareDocs/PrepareDocs.csproj" "./data/*.pdf" --storageendpoint $($env:AZURE_STORAGE_BLOB_ENDPOINT) --container $($env:AZURE_STORAGE_CONTAINER) --searchendpoint $($env:AZURE_SEARCH_SERVICE_ENDPOINT) --searchindex $($env:AZURE_SEARCH_INDEX) --openaiendpoint $($env:AZURE_OPENAI_ENDPOINT) --embeddingmodel $($env:AZURE_OPENAI_EMBEDDING_DEPLOYMENT) --formrecognizerendpoint $($env:AZURE_FORMRECOGNIZER_SERVICE_ENDPOINT) --tenantid $($env:AZURE_TENANT_ID) --verbose
"@
    
    $output = Invoke-ExternalCommand -Command "dotnet" -Arguments $dotnetArguments
    Write-Host $output

    Invoke-ExternalCommand -Command ($azdCmd).Source -Arguments @"
    env set AZD_PREPDOCS_RAN "true"
"@

}
else {
    Write-Host "AZD_PREPDOCS_RAN is set to true. Skipping the run."
}
