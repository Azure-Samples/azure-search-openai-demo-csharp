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

    args="--project "app/prepdocs/PrepareDocs/PrepareDocs.csproj" \
      './data/*.pdf' \
      --storageendpoint "$AZURE_STORAGE_BLOB_ENDPOINT" \
      --container "$AZURE_STORAGE_CONTAINER" \
      --searchendpoint "$AZURE_SEARCH_SERVICE_ENDPOINT" \
      --searchindex "$AZURE_SEARCH_INDEX" \
      --formrecognizerendpoint "$AZURE_FORMRECOGNIZER_SERVICE_ENDPOINT" \
      --tenantid "$AZURE_TENANT_ID""

    # if USE_VISION and AZURE_COMPUTERVISION_SERVICE_ENDPOINT is set, add --computervisionendpoint "$AZURE_COMPUTERVISION_SERVICE_ENDPOINT" to the command above
    if [ "$USE_VISION" = "true" ] && [ -n "$AZURE_COMPUTERVISION_SERVICE_ENDPOINT" ]; then
        args="$args --computervisionendpoint $AZURE_COMPUTERVISION_SERVICE_ENDPOINT"
    fi

    # if USE_AOAI is true, add --openaiendpoint "$AZURE_OPENAI_ENDPOINT" to the command above
    if [ "$USE_AOAI" = "true" ]; then
        echo "use azure openai"
        args="$args --openaiendpoint $AZURE_OPENAI_ENDPOINT"
        args="$args --embeddingmodel $AZURE_OPENAI_EMBEDDING_DEPLOYMENT"
    else
        echo "use openai"
        args="$args --embeddingmodel $OPENAI_EMBEDDING_DEPLOYMENT"
    fi

    args="$args --verbose"

    echo "Running: dotnet run $args"
    dotnet run $args

    azd env set AZD_PREPDOCS_RAN "true"
else
    echo "AZD_PREPDOCS_RAN is set to true. Skipping the run."
fi
