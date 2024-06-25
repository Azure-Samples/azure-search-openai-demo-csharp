#!/bin/sh

# Variables and secrets to remove
VARS_TO_REMOVE="AZURE_CLIENT_ID AZURE_ENV_NAME AZURE_TENANT_ID AZURE_LOCATION AZURE_RESOURCE_GROUP AZURE_SUBSCRIPTION_ID"
SECRETS_TO_REMOVE="AZURE_DEV_USER_AGENT AZURE_TAGS"

# Get organization and repository from command line arguments
ORG=$1
REPO=$2

# Function to delete variable
delete_env_var() {
  VAR_NAME=$1
  echo "Checking if variable $VAR_NAME exists..."
  if gh variable list -R $ORG/$REPO | grep -q $VAR_NAME; then
    echo "Deleting variable $VAR_NAME..."
    if ! gh variable delete $VAR_NAME -R $ORG/$REPO >/dev/null 2>&1; then
      echo "Failed to delete variable $VAR_NAME."
    else
      echo "Variable $VAR_NAME deleted."
    fi
  else
    echo "Variable $VAR_NAME does not exist, skipping..."
  fi
}

# Function to delete secret
delete_secret() {
  SECRET_NAME=$1
  echo "Checking if secret $SECRET_NAME exists..."
  if gh secret list -R $ORG/$REPO | grep -q $SECRET_NAME; then
    echo "Deleting secret $SECRET_NAME..."
    if ! gh secret delete $SECRET_NAME -R $ORG/$REPO >/dev/null 2>&1; then
      echo "Failed to delete secret $SECRET_NAME."
    else
      echo "Secret $SECRET_NAME deleted."
    fi
  else
    echo "Secret $SECRET_NAME does not exist, skipping..."
  fi
}

# Confirm deletion
echo "You are about to delete the following variables and secrets:"
echo "Variables: $VARS_TO_REMOVE"
echo "Secrets: $SECRETS_TO_REMOVE"
printf "Are you sure you want to continue? (Y/n) "
read REPLY
if [ -z "$REPLY" ] || [ "$REPLY" = "Y" ] || [ "$REPLY" = "y" ]
then
  for VAR_NAME in $VARS_TO_REMOVE
  do
    delete_env_var $VAR_NAME
  done

  for SECRET_NAME in $SECRETS_TO_REMOVE
  do
    delete_secret $SECRET_NAME
  done
fi
