#!/bin/sh

# Function to log messages
log_message() {
    echo "$(date +'%Y-%m-%d %H:%M:%S') - $1"
}

log_message "Script started."

output=$(azd env get-values)

IFS='
'
for line in $output; do
    name=$(printf "%s" "$line" | cut -d '=' -f 1)
    value=$(printf "%s" "$line" | cut -d '=' -f 2 | sed 's/^"//;s/"$//')
    export "$name=$value"
    log_message "Setting environment variable: $name=$value"
done

log_message "Environment variables set."

roles="
    5e0bd9bd-7b93-4f28-af87-19fc36ad61bd
    2a2b9908-6ea1-4ae2-8e65-a410df84e7d1
    ba92f5b4-2d11-453d-a403-e96b0029c9fe
    1407120a-92aa-4202-b7e9-c0e197c71c8f
    8ebe5a00-799e-43f5-93ac-243d3dce84a7
    7ca78c08-252a-4471-8644-bb5ff32d4ba0
    a97b65f3-24c7-4388-baec-2e87135dc908
"

# Check if required environment variables are set
missing_env_vars=""
for env_var in "AZURE_PRINCIPAL_ID" "AZURE_SUBSCRIPTION_ID" "AZURE_RESOURCE_GROUP"; do
    if [ -z "$(eval "echo \$$env_var")" ]; then
        if [ -n "$missing_env_vars" ]; then
            missing_env_vars="$missing_env_vars "
        fi
        missing_env_vars="$missing_env_vars$env_var"
    fi
done

if [ -n "$missing_env_vars" ]; then
    log_message "Error: The following required environment variables are not set: $missing_env_vars. Exiting..."
    if echo "$missing_env_vars" | grep -qw "AZURE_PRINCIPAL_ID"; then
        log_message "To get AZURE_PRINCIPAL_ID, you can run the following command: az ad signed-in-user show --query id"
    fi
    exit 1
fi

IFS='
'
for role in $roles; do
    role=$(echo "$role" | sed 's/^[[:space:]]*//;s/[[:space:]]*$//')
    log_message "Creating Azure role assignment for role: $role"
    az role assignment create \
        --role "$role" \
        --assignee-object-id "$AZURE_PRINCIPAL_ID" \
        --scope "/subscriptions/$AZURE_SUBSCRIPTION_ID/resourceGroups/$AZURE_RESOURCE_GROUP" \
        --assignee-principal-type User
done

log_message "Script finished."