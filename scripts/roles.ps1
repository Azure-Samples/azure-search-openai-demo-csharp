function Log-Message {
    param([string]$message)
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Write-Host "$timestamp - $message"
}

Log-Message "Script started."

$output = & azd env get-values

foreach ($line in $output) {
    $name, $value = $line.Split("=")
    $value = $value -replace '^\"|\"$'
    [Environment]::SetEnvironmentVariable($name, $value)
    Log-Message "Setting environment variable: $name=$value"
}

Log-Message "Environment variables set."

$roles = @(
    "5e0bd9bd-7b93-4f28-af87-19fc36ad61bd",
    "2a2b9908-6ea1-4ae2-8e65-a410df84e7d1",
    "ba92f5b4-2d11-453d-a403-e96b0029c9fe",
    "1407120a-92aa-4202-b7e9-c0e197c71c8f",
    "8ebe5a00-799e-43f5-93ac-243d3dce84a7",
    "7ca78c08-252a-4471-8644-bb5ff32d4ba0",
    "a97b65f3-24c7-4388-baec-2e87135dc908"
)

if ([string]::IsNullOrEmpty($env:AZURE_RESOURCE_GROUP)) {
    $env:AZURE_RESOURCE_GROUP = "rg-$env:AZURE_ENV_NAME"
    & azd env set AZURE_RESOURCE_GROUP $env:AZURE_RESOURCE_GROUP
}

# Check if required environment variables are set
$requiredEnvVars = @("AZURE_PRINCIPAL_ID", "AZURE_SUBSCRIPTION_ID", "AZURE_RESOURCE_GROUP")
$missingEnvVars = @()

foreach ($envVar in $requiredEnvVars) {
    if (-not (Test-Path env:$envVar)) {
        $missingEnvVars += $envVar
    }
}

if ($missingEnvVars.Count -gt 0) {
    Log-Message "Error: The following required environment variables are not set: $($missingEnvVars -join ', '). Exiting..."
    if ($missingEnvVars -contains "AZURE_PRINCIPAL_ID") {
        Log-Message "To get AZURE_PRINCIPAL_ID, you can run the following command: az ad signed-in-user show --query id"
    }
    exit 1
}

foreach ($role in $roles) {
    $role = $role.Trim()
    Log-Message "Creating Azure role assignment for role: $role"
    & az role assignment create `
        --role $role `
        --assignee-object-id $env:AZURE_PRINCIPAL_ID `
        --scope "/subscriptions/$env:AZURE_SUBSCRIPTION_ID/resourceGroups/$env:AZURE_RESOURCE_GROUP" `
        --assignee-principal-type User
}

Log-Message "Script finished."
