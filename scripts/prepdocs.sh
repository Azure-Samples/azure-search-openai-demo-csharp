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

echo 'Running "PrepareDocs.dll"'
dotnet run --project "app/prepdocs/PrepareDocs/PrepareDocs.csproj" -- \
  './data/*.pdf' \
  --storageaccount "$AZURE_STORAGE_ACCOUNT" \
  --container "$AZURE_STORAGE_CONTAINER" \
  --searchservice "$AZURE_SEARCH_SERVICE" \
  --index "$AZURE_SEARCH_INDEX" \
  --formrecognizerservice "$AZURE_FORMRECOGNIZER_SERVICE" \
  --tenantid "$AZURE_TENANT_ID" \
  -v
