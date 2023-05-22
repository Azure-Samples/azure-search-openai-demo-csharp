#!/bin/sh

echo ""
echo "Loading azd .env file from current environment"
echo ""

while IFS='=' read -r key value; do
    value=$(echo "$value" | sed 's/^"//' | sed 's/"$//')
    export "$key=$value"
done <<EOF
$(azd env get-values)
EOF

echo "Environment variables set."

if [ -z "$AZD_PREPDOCS_RAN" ] || [ "$AZD_PREPDOCS_RAN" = "false" ]; then
    echo 'Running "PrepareDocs.dll"'

    pwd

    dotnet run --project "app/prepdocs/PrepareDocs/PrepareDocs.csproj" -- \
      './data/*.pdf' \
      --storageendpoint "$AZURE_STORAGE_BLOB_ENDPOINT" \
      --container "$AZURE_STORAGE_CONTAINER" \
      --searchendpoint "$AZURE_SEARCH_SERVICE_ENDPOINT" \
      --searchindex "$AZURE_SEARCH_INDEX" \
      --formrecognizerendpoint "$AZURE_FORMRECOGNIZER_SERVICE_ENDPOINT" \
      --tenantid "$AZURE_TENANT_ID" \
      -v

    azd env set AZD_PREPDOCS_RAN "true"
else
    echo "AZD_PREPDOCS_RAN is set to true. Skipping the run."
fi
