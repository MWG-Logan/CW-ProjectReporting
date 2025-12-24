# Architecture

## Overview
The solution consists of three projects:
- Bezalu.ProjectReporting.Web (Blazor WebAssembly front end)
- Bezalu.ProjectReporting.API (Azure Functions isolated worker back end)
- Bezalu.ProjectReporting.Shared (DTO contracts shared by both)

## Data Flow
1. User accesses Blazor WebAssembly app via Azure Static Web Apps.
2. Static Web Apps enforces authentication via Azure AD (Entra ID).
3. User enters Project ID in Web UI.
4. Front end POSTs projectId to `/api/reports/project-completion` (with SWA auth cookies).
5. SWA validates authentication and forwards request to Azure Functions.
6. API fetches project, phases, tickets, notes from ConnectWise.
7. API builds `ProjectCompletionReportResponse` and invokes Azure OpenAI to produce `AiGeneratedSummary`.
8. JSON returned to client; client renders summary markdown using Markdig.
9. User initiates PDF download; front end POSTs the full report JSON to `/api/reports/project-completion/pdf`.
10. API composes PDF using QuestPDF with supplied data (skip re-fetch & AI).
11. Client receives PDF bytes and triggers browser download via JS interop.

## Key Services
- `IConnectWiseApiClient`: wraps HTTP calls to ConnectWise endpoints.
- `IProjectReportingService`: orchestrates data retrieval, report building, AI prompt assembly.
- `IAzureOpenAIService`: abstraction over Azure OpenAI ChatClient for summary generation.

## Design Choices
- DTO reuse avoids duplication between front end and API.
- POST for PDF avoids second expensive aggregation call.
- Markdown + Markdig chosen for flexibility in AI summary formatting.
- QuestPDF chosen for deterministic server-side PDF rendering.
- **Azure Static Web Apps** provides integrated hosting, authentication, and API routing.
- **Function AuthorizationLevel.Anonymous** used because authentication is enforced upstream by SWA.

## Authentication Architecture
- **Static Web Apps** handles authentication via Azure AD (Entra ID)
- `staticwebapp.config.json` enforces `authenticated` role for `/api/*` routes
- Unauthenticated users redirected to `/.auth/login/aad`
- SWA session cookies automatically included in API requests
- Azure Functions trust SWA authentication layer - no function keys needed

## Future Enhancements
- Caching of raw ConnectWise responses.
- Additional export formats (Excel, HTML full report).
- User-level access control based on ConnectWise permissions.
- Visual charts for variance trends and project analytics.
