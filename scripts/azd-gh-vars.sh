#!/bin/sh

# Check if the correct number of arguments are passed
if [ $# -ne 3 ]; then
    echo "Usage: $0 <azd_env_name> <github_org> <github_repo>"
    exit 1
fi

AZD_ENV_NAME=$1
GH_ORG=$2
GH_REPO=$3

echo "AZD environment: $AZD_ENV_NAME"
echo "GitHub org: $GH_ORG"
echo "GitHub repo: $GH_REPO"

# Check if the .env file exists
if [ ! -f ".azure/$AZD_ENV_NAME/.env" ]; then
    echo "Error: .env file not found in .azure/$AZD_ENV_NAME/"
    exit 1
fi

# Define the list of environment variables to be used
VAR_LIST="AZURE_OPENAI_SERVICE AZURE_OPENAI_RESOURCE_GROUP AZURE_FORMRECOGNIZER_SERVICE AZURE_FORMRECOGNIZER_RESOURCE_GROUP AZURE_SEARCH_SERVICE AZURE_SEARCH_SERVICE_RESOURCE_GROUP AZURE_STORAGE_ACCOUNT AZURE_STORAGE_RESOURCE_GROUP AZURE_KEY_VAULT_NAME AZURE_KEY_VAULT_RESOURCE_GROUP SERVICE_WEB_IDENTITY_NAME"

echo "Variables to copy: $VAR_LIST"

# Create or overwrite the .env-gh-vars file
: > ".azure/$AZD_ENV_NAME/.env-gh-vars"
echo "Created/overwritten .env-gh-vars file in .azure/$AZD_ENV_NAME/"

# Read the .env file and write the matches to the new .env file
while IFS='=' read -r var value
do
    for VAR in $VAR_LIST
    do
        if [ "$var" = "$VAR" ]; then
            echo "$var=$value" >> ".azure/$AZD_ENV_NAME/.env-gh-vars"
            echo "Copied $var to .env-gh-vars"
        fi
    done
done < ".azure/$AZD_ENV_NAME/.env"

echo "Finished copying variables to .env-gh-vars"

# Call the gh variable set command
echo "Setting GitHub variables..."
gh variable set --env-file ".azure/$AZD_ENV_NAME/.env-gh-vars" -R "$GH_ORG/$GH_REPO"
echo "GitHub variables set"
