# Front-End (Blazor WebAssembly)

## Key Behaviors
- User enters Project ID; triggers report fetch.
- Displays metrics in Fluent UI components (cards, tabs, data grids, accordion).
- AI summary rendered as markdown using Markdig.
- PDF download posts existing report JSON to API; no recomputation.

## State Management
- Local component state only (no global store yet).
- `IsLoading` for initial report, `IsPdfLoading` for PDF call.

## HTTP
- `HttpClient` base address from host environment; assumes reverse proxy or relative `/api` route.

## File Download
- `saveFile` JS helper converts Base64 to Blob and triggers `<a download>`.

## Extensibility
- Add charts (variance trends) via a chart library.
- Add caching in browser (localStorage) for last report.
- Add global error boundary for API failures.
