# Front-End (Blazor WebAssembly)

## Overview
The front-end is a Blazor WebAssembly application using Microsoft Fluent UI components, deployed on Azure Static Web Apps with integrated Azure AD authentication.

## Key Behaviors
- User must authenticate via Azure AD before accessing the application.
- User enters Project ID; triggers report fetch.
- Displays metrics in Fluent UI components (cards, tabs, data grids, accordion).
- AI summary rendered as markdown using Markdig.
- PDF download posts existing report JSON to API; no recomputation.

## Authentication
- **Azure Static Web Apps** handles authentication via Azure AD (Entra ID)
- Configured in `staticwebapp.config.json`:
  - `/api/*` routes require `authenticated` role
  - Unauthenticated users redirected to `/.auth/login/aad`
- SWA session cookies automatically included in API requests
- No additional token management needed in Blazor code

## State Management
- Local component state only (no global store yet).
- `IsLoading` for initial report, `IsPdfLoading` for PDF call.

## HTTP
- `HttpClient` base address from host environment.
- Assumes SWA reverse proxy routing for `/api` routes.
- SWA authentication cookies automatically included in requests.
- No manual authorization headers needed.

## File Download
- `saveFile` JS helper converts Base64 to Blob and triggers `<a download>`.

## Extensibility
- Add charts (variance trends) via a chart library.
- Add caching in browser (localStorage) for last report.
- Add global error boundary for API failures.
