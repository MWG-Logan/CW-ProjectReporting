# Project Reporting Application

Full solution providing interactive project completion reporting for ConnectWise Manage with AI summary and PDF export.

## Components

- Web Front-End (Blazor WebAssembly) served as static assets (Azure Static Web Apps).
- Serverless API (Azure Functions isolated worker) for data aggregation and AI summary.
- Shared DTO library for consistent contracts.
- PDF generation via QuestPDF using already fetched report payload (no regeneration).

## End-to-End Flow
1. User enters Project ID and triggers `POST /api/reports/project-completion`.
2. API aggregates ConnectWise data, builds `ProjectCompletionReportResponse`, calls Azure OpenAI for AI summary, returns JSON.
3. Front-end displays report (timeline, budget, phases, tickets, AI summary rendered as Markdown).
4. User clicks Download PDF; front-end posts the existing JSON report body to `POST /api/reports/project-completion/pdf` (avoids second data + AI call). API returns PDF bytes.
5. Front-end uses JS interop helper (`saveFile`) to download the PDF with filename `project-{id}-completion-report.pdf`.

## Primary API Endpoints

### POST /api/reports/project-completion
Request body:
```
{ "projectId":12345 }
```
Response: `ProjectCompletionReportResponse` (see docs/contract.md).

### POST /api/reports/project-completion/pdf
Request body: full `ProjectCompletionReportResponse` previously returned by the first endpoint.
Response: `application/pdf` file.

## Report Object Highlights
- Summary (status, dates, manager, company)
- TimelineAnalysis (planned vs actual days, variance, adherence, performance %)
- BudgetAnalysis (estimated vs actual hours, variance, adherence)
- Phases list (per phase hours + status)
- Tickets list (meta, hours, notes subset)
- AiGeneratedSummary (markdown rendered client-side with Markdig)

## Operability Notes
- PDF endpoint expects complete report payload; front-end must retain JSON until download.
- AI call cost avoided for PDF generation due to reuse of existing summary.
- Truncation applied to notes for AI prompt and PDF to control size.

## Local Development
Prerequisites:
- .NET SDK
- Azure Functions Core Tools
- Valid ConnectWise + Azure OpenAI credentials in `local.settings.json` (excluded from source control).

Run API:
```
func start
```
Run front-end:
```
dotnet run --project Bezalu.ProjectReporting.Web
```
Front-end will proxy to API according to Static Web Apps configuration/emulator or manual CORS settings if needed.

## Azure Deployment Overview
- Deploy Blazor WebAssembly output (Release) to Azure Static Web Apps.
- Deploy Azure Functions project to same Static Web Apps resource (api folder) or separate Functions App (configure SWA `api_location`).
- Set configuration (App Settings) for ConnectWise and Azure OpenAI keys; prefer Key Vault references in production.

Required App Settings:
- `ConnectWise:BaseUrl`, `ConnectWise:CompanyId`, `ConnectWise:PublicKey`, `ConnectWise:PrivateKey`, `ConnectWise:ClientId`
- `AzureOpenAI:Endpoint`, `AzureOpenAI:DeploymentName` (credential via `DefaultAzureCredential` in code; ensure managed identity / RBAC permissions)

## Performance & Size
- WASM project uses trimming + AOT for faster runtime once cached; consider disabling AOT in Debug for faster builds.
- AI prompt size limited by truncation strategies in service.

## Error Handling
-400 invalid project id or invalid report payload for PDF endpoint.
-404 project not found.
-500 unexpected processing errors.

## Security
- Function auth level currently `Function`; set keys or add front-end auth (e.g., Entra ID) before production.
- Do not send sensitive data inside report payload for PDF endpoint; only project analysis data.

## Extensibility
- Add cached layer to reuse raw data for multiple exports.
- Extend PDF sections (charts) by computing aggregates client-side and passing them in extended DTO.
- Add Excel export by introducing another POST /api/reports/project-completion/excel endpoint using a spreadsheet library server-side.

## Documentation
See `/docs` for deeper details:
- architecture.md (layer & data flow)
- contract.md (DTO shapes)
- pdf.md (PDF composition logic)
- deployment.md (Azure setup steps)
- frontend.md (UI behaviors)

---
This README targets the operational overview; for detailed structures consult docs directory.
