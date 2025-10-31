# Architecture

## Overview
The solution consists of three projects:
- Bezalu.ProjectReporting.Web (Blazor WebAssembly front end)
- Bezalu.ProjectReporting.API (Azure Functions isolated worker back end)
- Bezalu.ProjectReporting.Shared (DTO contracts shared by both)

## Data Flow
1. User enters Project ID in Web UI.
2. Front end POSTs projectId to `/api/reports/project-completion`.
3. API fetches project, phases, tickets, notes from ConnectWise.
4. API builds `ProjectCompletionReportResponse` and invokes Azure OpenAI to produce `AiGeneratedSummary`.
5. JSON returned to client; client renders summary markdown using Markdig.
6. User initiates PDF download; front end POSTs the full report JSON to `/api/reports/project-completion/pdf`.
7. API composes PDF using QuestPDF with supplied data (skip re-fetch & AI).
8. Client receives PDF bytes and triggers browser download via JS interop.

## Key Services
- `IConnectWiseApiClient`: wraps HTTP calls to ConnectWise endpoints.
- `IProjectReportingService`: orchestrates data retrieval, report building, AI prompt assembly.
- `IAzureOpenAIService`: abstraction over Azure OpenAI ChatClient for summary generation.

## Design Choices
- DTO reuse avoids duplication between front end and API.
- POST for PDF avoids second expensive aggregation call.
- Markdown + Markdig chosen for flexibility in AI summary formatting.
- QuestPDF chosen for deterministic server-side PDF rendering.

## Future Enhancements
- Caching of raw ConnectWise responses.
- Additional export formats (Excel, HTML full report).
- Authentication (OIDC) integration for user-level access.
