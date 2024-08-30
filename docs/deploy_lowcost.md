# Deploying with Minimal Costs

This AI RAG chat application is designed to be easily deployed using the Azure Developer CLI, which provisions the infrastructure according to the Bicep files in the `infra` folder. Those files describe each of the Azure resources needed, and configures their SKU (pricing tier) and other parameters. Many Azure services offer a free tier, but the infrastructure files in this project do *not* default to the free tier as there are often limitations in that tier.

However, if your goal is to minimize costs while prototyping your application, follow the steps below *before* running `azd up`. Once you've gone through these steps, return to the [deployment steps](../README.md#deployment).

[📺 Live stream: Deploying from a free account](https://youtu.be/V1ZLzXU4iiw)

1. Log in to your Azure account using the Azure Developer CLI:

    ```shell
    azd auth login
    ```

1. Create a new azd environment for the free resource group:

    ```shell
    azd env new
    ```

    Enter a name that will be used for the resource group.
    This will create a new folder in the `.azure` folder, and set it as the active environment for any calls to `azd` going forward.

1. Use the free tier of **Azure AI Document Intelligence** (previously known as [Form Recognizer](https://learn.microsoft.com/en-us/azure/ai-services/document-intelligence/overview?view=doc-intel-4.0.0)):

    ```shell
    azd env set AZURE_FORMRECOGNIZER_SERVICE_SKU F0
    ```

1. Use the free tier of **Azure AI Search**:

    ```shell
    azd env set AZURE_SEARCH_SERVICE_SKU free
    azd env set AZURE_SEARCH_SEMANTIC_RANKER disabled
    ```

    Limitations:
    1. You are only allowed one free search service across all regions.
    If you have one already, either delete that service or follow instructions to
    reuse your [existing search service](../README.md#use-existing-resources).
    2. The free tier does not support semantic ranker. Note that will generally result in [decreased search relevance](https://techcommunity.microsoft.com/t5/ai-azure-ai-services-blog/azure-ai-search-outperforming-vector-search-with-hybrid/ba-p/3929167).

1. Turn off **Azure Monitor** (Application Insights):

    ```shell
    azd env set AZURE_USE_APPLICATION_INSIGHTS false
    ```

    Application Insights is quite inexpensive already, so turning this off may not be worth the costs saved, but it is an option for those who want to minimize costs.

1. (Optional) Use **OpenAI.com** instead of Azure OpenAI.

    You can create a free account in OpenAI and [request a key to use OpenAI models](https://platform.openai.com/docs/quickstart/create-and-export-an-api-key). Once you have this, you can disable the use of Azure OpenAI Services, and use OpenAI APIs.

    ```shell
    azd env set USE_AOAI false
    azd env set USE_VISION false
    azd env set OPENAI_CHATGPT_DEPLOYMENT gpt-4o-mini
    azd env set OPENAI_API_KEY <your openai.com key goes here>    
    ```

    ***Note:** Both Azure OpenAI and openai.com OpenAI accounts will incur costs, based on tokens used, but the costs are fairly low for the amount of sample data (less than $10).*

1. Once you've made the desired customizations, follow the steps in the README [to run `azd up`](../README.md#deploying-from-scratch). We recommend using "eastus" as the region, for availability reasons.