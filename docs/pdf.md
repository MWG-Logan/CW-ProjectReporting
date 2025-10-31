# PDF Generation

## Endpoint
`POST /api/reports/project-completion/pdf`
Body: full `ProjectCompletionReportResponse` JSON.

## Rationale
- Avoids re-fetching ConnectWise data.
- Skips second AI summary generation (cost + latency).
- Ensures PDF matches on-screen data exactly.

## Implementation
- QuestPDF used in Azure Functions isolated worker.
- License set to Community.
- Sections rendered: Header, Summary, Timeline, Budget, AI Summary, Phases, Tickets, Footer with page numbers.

## Size Control
- Only top N (10) notes per ticket included.
- AI summary included as plain text (markdown not interpreted server-side).

## Extensibility
- Add charts (hours variance) via `Canvas` or table sections.
- Add cover page: introduce first `Page` before current layout.
- Support custom branding: pass logo URL in request and embed image.

## Client Download
- Blazor WebAssembly posts report JSON.
- Receives `application/pdf` bytes.
- JS interop `saveFile` creates Blob and triggers download.
