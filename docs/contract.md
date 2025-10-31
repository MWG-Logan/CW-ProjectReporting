# Report Contract (DTOs)

## ProjectCompletionReportRequest
```jsonc
{
 "projectId":12345
}
```

## ProjectCompletionReportResponse
```jsonc
{
 "projectId":12345,
 "projectName": "Example Project",
 "summary": {
 "status": "Completed",
 "actualStart": "2024-01-01T00:00:00Z",
 "actualEnd": "2024-03-31T00:00:00Z",
 "plannedStart": "2024-01-01T00:00:00Z",
 "plannedEnd": "2024-03-15T00:00:00Z",
 "manager": "John Doe",
 "company": "Acme Corp"
 },
 "timeline": {
 "totalDays":90,
 "plannedDays":74,
 "varianceDays":16,
 "scheduleAdherence": "Behind Schedule",
 "schedulePerformance": "121.6%"
 },
 "budget": {
 "estimatedHours":500.0,
 "actualHours":550.0,
 "varianceHours":50.0,
 "estimatedCost":0,
 "actualCost":0,
 "varianceCost":0,
 "budgetAdherence": "Slightly Over",
 "costPerformance": "110.0%"
 },
 "phases": [
 {
 "phaseId":1,
 "phaseName": "Planning",
 "status": "Complete",
 "actualStart": "2024-01-01T00:00:00Z",
 "actualEnd": "2024-01-05T00:00:00Z",
 "estimatedHours":40.0,
 "actualHours":42.5,
 "notes": ["Initial kickoff done"],
 "summary": null
 }
 ],
 "tickets": [
 {
 "ticketId":100,
 "ticketNumber": "123456",
 "summary": "Implement feature X",
 "status": "Closed",
 "type": "Development",
 "subType": "Enhancement",
 "estimatedHours":16.0,
 "actualHours":18.25,
 "notes": ["Reviewed by QA", "Minor fixes"]
 }
 ],
 "aiGeneratedSummary": "...markdown text...",
 "generatedAt": "2024-10-29T04:00:00Z"
}
```

## Notes
- Ticket and phase notes truncated for AI prompt & PDF.
- `AiGeneratedSummary` is markdown; client renders safely via Markdig.
