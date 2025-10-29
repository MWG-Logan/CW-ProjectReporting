# Bezalu.ProjectReporting

A comprehensive project reporting solution that integrates with ConnectWise Manage to generate AI-powered project completion reports.

## Overview

This solution consists of two main projects:

1. **Bezalu.ProjectReporting.API** - Azure Functions backend API that generates project completion reports
2. **Bezalu.ProjectReporting.Web** - Blazor WebAssembly frontend (not yet implemented)

## Features

### Project Completion Reports

Generate detailed project completion reports that include:

- **Timeline Analysis**: Compare planned vs actual project duration with variance calculations
- **Budget Analysis**: Track estimated vs actual hours with budget adherence metrics
- **Phase Breakdown**: Detailed information for each project phase including notes
- **Ticket Summaries**: Complete ticket information with status, hours, and notes
- **AI-Powered Insights**: Azure OpenAI generates comprehensive summaries analyzing:
  - Ticket and phase action completion
  - Time budget adherence
  - Schedule adherence to planned start/end dates
  - Notes quality compared to actual task completion
  - Overall project performance assessment
  - Recommendations and insights

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- Azure Functions Core Tools (for local development)
- ConnectWise Manage API credentials
- Azure OpenAI resource and API key

### Configuration

1. Navigate to `Bezalu.ProjectReporting.API`
2. Copy `local.settings.json` and update with your credentials:
   - ConnectWise API credentials (company ID, public/private keys, client ID)
   - Azure OpenAI endpoint and API key

See [API README](./Bezalu.ProjectReporting.API/README.md) for detailed configuration instructions.

### Running Locally

```bash
cd Bezalu.ProjectReporting.API
func start
```

### Testing the API

```bash
curl -X POST http://localhost:7071/api/reports/project-completion \
  -H "Content-Type: application/json" \
  -d '{"projectId": 12345}'
```

## API Endpoints

### POST /api/reports/project-completion

Generates a comprehensive project completion report for the specified project ID.

**Request Body:**
```json
{
  "projectId": 12345
}
```

**Response:** Returns a detailed JSON report including project summary, timeline analysis, budget analysis, phase details, ticket summaries, and AI-generated insights. See the [API README](./Bezalu.ProjectReporting.API/README.md) for complete response schema.

## Architecture

### Backend (API)

- **Azure Functions** with Isolated Worker Model
- **.NET 9.0** runtime
- **Service Layer Architecture**:
  - `IConnectWiseApiClient`: HTTP client wrapper for ConnectWise Manage API
  - `IAzureOpenAIService`: Azure OpenAI integration for generating summaries
  - `IProjectReportingService`: Orchestrates data gathering and report generation
- **Dependency Injection**: All services registered and configured in Program.cs
- **DTOs**: Clean separation between API contracts and internal models

### Frontend (Web)

- **Blazor WebAssembly** (to be implemented)
- Will consume the API to display and generate PDF reports

## Project Structure

```
CW-ProjectReporting/
├── Bezalu.ProjectReporting.API/
│   ├── DTOs/                    # Data Transfer Objects
│   ├── Functions/               # Azure Function endpoints
│   ├── Models/                  # ConnectWise entity models
│   ├── Services/                # Business logic and API clients
│   ├── Program.cs               # Startup and DI configuration
│   ├── host.json               # Azure Functions host configuration
│   ├── local.settings.json     # Local development settings (not in git)
│   └── README.md               # Detailed API documentation
└── Bezalu.ProjectReporting.Web/
    └── (Blazor WebAssembly project - to be implemented)
```

## Security

- API keys and credentials stored in configuration (use Azure Key Vault in production)
- `local.settings.json` excluded from source control
- Function-level authorization required for API endpoints
- No vulnerabilities detected by CodeQL security scanning

## Development Notes

- All services use dependency injection for testability
- Comprehensive error handling and logging
- Async/await throughout for optimal performance
- Clean architecture with separation of concerns

## Future Enhancements

- Implement Blazor frontend for report visualization
- Add PDF generation capability
- Implement additional report types
- Add caching for improved performance
- Add unit and integration tests
- Implement batch report generation

## Documentation

- [API Documentation](./Bezalu.ProjectReporting.API/README.md) - Detailed API documentation, configuration, and usage
- ConnectWise API Reference: https://developer.connectwise.com/

## License

See LICENSE.txt for details.