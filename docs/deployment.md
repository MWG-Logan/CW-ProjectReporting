# Deployment

## Azure Static Web Apps + Azure Functions
- Front end (Blazor WASM) deployed to Static Web Apps.
- API (Azure Functions) integrated via SWA's managed Functions (api folder) or linked to separate Functions App.
- **Authentication** handled by Static Web Apps with Azure AD integration.

## Steps (Integrated Deployment)
1. Create Azure Static Web App resource in Azure Portal.
2. Configure Azure AD authentication provider in SWA settings.
3. Set `api_location` to point to Azure Functions project folder during SWA build.
4. Configure Application Settings for ConnectWise + Azure OpenAI in SWA or linked Functions App.
5. Deploy via GitHub Actions (auto-configured by SWA) or manual deployment:
   - Front-end: `dotnet publish -c Release Bezalu.ProjectReporting.Web`
   - API: Functions automatically deployed with SWA or via `func azure functionapp publish <name>`

## Steps (Separate Functions App)
1. Create Azure Functions App (Isolated .NET runtime) and configure Application Settings for ConnectWise + Azure OpenAI.
2. Deploy API project via `func azure functionapp publish <name>` or CI.
3. Create Static Web App and link to external Functions App or configure front end to call external API base URL.
4. Configure Azure AD authentication in Static Web Apps settings.
5. Upload front-end build output to SWA.

## Configuration Keys
- `ConnectWise:*`
- `AzureOpenAI:Endpoint`
- `AzureOpenAI:DeploymentName`

## Authentication
- **Azure Static Web Apps** integrated with **Azure AD (Entra ID)** for authentication
- Configure authentication provider in Azure Portal under SWA > Settings > Authentication
- `staticwebapp.config.json` enforces authentication:
  - `/api/*` routes require `authenticated` role
  - Unauthenticated users redirected to `/.auth/login/aad`
- No function keys needed - Azure Functions use `AuthorizationLevel.Anonymous` and trust SWA authentication
- Session managed by SWA; cookies automatically included in API requests

## Environment Segregation
- Use separate resource groups for dev/stage/prod.
- Use managed identity for Azure OpenAI credentials instead of API key.

## Logging & Monitoring
- Application Insights configured in API project.
- Add custom telemetry events for PDF generation latency.

## CDN / Performance
- Enable SWA global distribution.
- WASM trimming + AOT already enabled (consider testing cold starts).
