# Next Steps after `azd init`

## Table of Contents

1. [Next Steps](#next-steps)
2. [What was added](#what-was-added)
3. [Billing](#billing)
4. [Troubleshooting](#troubleshooting)

## Next Steps

### Provision infrastructure and deploy application code

Run `azd up` to provision your infrastructure and deploy to Azure (or run `azd provision` then `azd deploy` to accomplish the tasks separately). Visit the service endpoints listed to see your application up-and-running!

To troubleshoot any issues, see [troubleshooting](#troubleshooting).

### Configure environment variables for running services

Configure environment variables for running services by updating `settings` in [main.parameters.json](./infra/main.parameters.json).

### Configure CI/CD pipeline

1. Create a workflow pipeline file locally. The following starters are available:
   - [Deploy with GitHub Actions](https://github.com/Azure-Samples/azd-starter-bicep/blob/main/.github/workflows/azure-dev.yml)
   - [Deploy with Azure Pipelines](https://github.com/Azure-Samples/azd-starter-bicep/blob/main/.azdo/pipelines/azure-dev.yml)
2. Run `azd pipeline config` to configure the deployment pipeline to connect securely to Azure.

## What was added

### Infrastructure configuration

To describe the infrastructure and application, `azure.yaml` along with Infrastructure as Code files using Bicep were added with the following directory structure:

```yaml
- azure.yaml     # azd project configuration
- infra/         # Infrastructure as Code (bicep) files
  - main.bicep   # main deployment module
  - app/         # Application resource modules
  - shared/      # Shared resource modules
  - modules/     # Library modules
```

Each bicep file declares resources to be provisioned. The resources are provisioned when running `azd up` or `azd provision`.

- [app/backend.bicep](./infra/app/backend.bicep) - Azure Container Apps resources to host the 'backend' service.
- [app/EmbedFunctions.bicep](./infra/app/EmbedFunctions.bicep) - Azure Container Apps resources to host the 'EmbedFunctions' service.
- [app/PrepareDocs.bicep](./infra/app/PrepareDocs.bicep) - Azure Container Apps resources to host the 'PrepareDocs' service.
- [shared/keyvault.bicep](./infra/shared/keyvault.bicep) - Azure KeyVault to store secrets.
- [shared/monitoring.bicep](./infra/shared/monitoring.bicep) - Azure Log Analytics workspace and Application Insights to log and store instrumentation logs.
- [shared/registry.bicep](./infra/shared/registry.bicep) - Azure Container Registry to store docker images.

More information about [Bicep](https://aka.ms/bicep) language.

### Build from source (no Dockerfile)

#### Build with Buildpacks using Oryx

If your project does not contain a Dockerfile, we will use [Buildpacks](https://buildpacks.io/) using [Oryx](https://github.com/microsoft/Oryx/blob/main/doc/README.md) to create an image for the services in `azure.yaml` and get your containerized app onto Azure.

To produce and run the docker image locally:

1. Run `azd package` to build the image.
2. Copy the *Image Tag* shown.
3. Run `docker run -it <Image Tag>` to run the image locally.

#### Exposed port

Oryx will automatically set `PORT` to a default value of `80` (port `8080` for Java). Additionally, it will auto-configure supported web servers such as `gunicorn` and `ASP .NET Core` to listen to the target `PORT`. If your application already listens to the port specified by the `PORT` variable, the application will work out-of-the-box. Otherwise, you may need to perform one of the steps below:

1. Update your application code or configuration to listen to the port specified by the `PORT` variable
1. (Alternatively) Search for `targetPort` in a .bicep file under the `infra/app` folder, and update the variable to match the port used by the application.

## Billing

Visit the *Cost Management + Billing* page in Azure Portal to track current spend. For more information about how you're billed, and how you can monitor the costs incurred in your Azure subscriptions, visit [billing overview](https://learn.microsoft.com/azure/developer/intro/azure-developer-billing).

## Troubleshooting

Q: I visited the service endpoint listed, and I'm seeing a blank page, a generic welcome page, or an error page.

A: Your service may have failed to start, or it may be missing some configuration settings. To investigate further:

1. Run `azd show`. Click on the link under "View in Azure Portal" to open the resource group in Azure Portal.
2. Navigate to the specific Container App service that is failing to deploy.
3. Click on the failing revision under "Revisions with Issues".
4. Review "Status details" for more information about the type of failure.
5. Observe the log outputs from Console log stream and System log stream to identify any errors.
6. If logs are written to disk, use *Console* in the navigation to connect to a shell within the running container.

For more troubleshooting information, visit [Container Apps troubleshooting](https://learn.microsoft.com/azure/container-apps/troubleshooting). 

### Additional information

For additional information about setting up your `azd` project, visit our official [docs](https://learn.microsoft.com/azure/developer/azure-developer-cli/make-azd-compatible?pivots=azd-convert).
