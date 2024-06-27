#!/bin/sh

# Check if the correct number of arguments are passed
if [ $# -ne 2 ]; then
    echo "Usage: $0 <source_env_name> <dest_env_name>"
    exit 1
fi

SOURCE_ENV_NAME=$1
DEST_ENV_NAME=$2

echo "Source environment: $SOURCE_ENV_NAME"
echo "Destination environment: $DEST_ENV_NAME"

# Check if the source .env file exists
if [ ! -f ".azure/$SOURCE_ENV_NAME/.env" ]; then
    echo "Error: .env file not found in .azure/$SOURCE_ENV_NAME/"
    exit 1
fi

# Define the list of environment variables to be used
VAR_LIST="AZURE_OPENAI_SERVICE AZURE_OPENAI_RESOURCE_GROUP AZURE_FORMRECOGNIZER_SERVICE AZURE_FORMRECOGNIZER_RESOURCE_GROUP AZURE_SEARCH_SERVICE AZURE_SEARCH_SERVICE_RESOURCE_GROUP AZURE_STORAGE_ACCOUNT AZURE_STORAGE_RESOURCE_GROUP AZURE_KEY_VAULT_NAME AZURE_KEY_VAULT_RESOURCE_GROUP SERVICE_WEB_IDENTITY_NAME"

echo "Variables to copy: $VAR_LIST"

# Read the source .env file and write the matches to the destination .env file
while IFS='=' read -r var value
do
    for VAR in $VAR_LIST
    do
        if [ "$var" = "$VAR" ]; then
            # Remove the variable if it already exists in the destination .env file
            sed -i "/^$VAR=/d" ".azure/$DEST_ENV_NAME/.env"
            # Write the variable to the destination .env file
            echo "$var=$value" >> ".azure/$DEST_ENV_NAME/.env"
            echo "Copied $var to destination environment"
        fi
    done
done < ".azure/$SOURCE_ENV_NAME/.env"

echo "Finished copying variables"
