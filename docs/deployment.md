# Deployment

## Azure Static Web Apps + Azure Functions
- Front end (Blazor WASM) deployed to Static Web Apps.
- API (Azure Functions) either integrated (api folder) or separate Functions App.

## Steps (Separate Functions App)
1. Create Azure Functions App (Isolated .NET runtime) and configure Application Settings for ConnectWise + Azure OpenAI.
2. Deploy API project via `func azure functionapp publish <name>` or CI.
3. Create Static Web App and set `api_location` to Functions App if integrated, else configure front end to call external API base URL.
4. Upload front-end build (`dotnet publish -c Release Bezalu.ProjectReporting.Web`).

## Configuration Keys
- `ConnectWise:*`
- `AzureOpenAI:Endpoint`
- `AzureOpenAI:DeploymentName`

## Authentication
- Add Entra ID or other auth on Static Web Apps; issue front-end access token; secure Functions with Easy Auth or custom.

## Environment Segregation
- Use separate resource groups for dev/stage/prod.
- Use managed identity for Azure OpenAI credentials instead of API key.

## Logging & Monitoring
- Application Insights configured in API project.
- Add custom telemetry events for PDF generation latency.

## CDN / Performance
- Enable SWA global distribution.
- WASM trimming + AOT already enabled (consider testing cold starts).
