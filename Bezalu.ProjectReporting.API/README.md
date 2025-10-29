# Project Reporting API

This API provides project completion reporting functionality for ConnectWise Manage projects, with AI-powered analysis using Azure OpenAI.

## Features

- **Project Completion Reports**: Generate comprehensive reports analyzing project completion, including:
  - Timeline analysis (planned vs actual dates)
  - Budget analysis (planned vs actual hours)
  - Phase-by-phase breakdown
  - Ticket summaries with notes
  - AI-generated executive summary and insights

## API Endpoints

### Generate Project Completion Report

**Endpoint**: `POST /api/reports/project-completion`

**Request Body**:
```json
{
  "projectId": 12345
}
```

**Response**:
```json
{
  "projectId": 12345,
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
    "totalDays": 90,
    "plannedDays": 74,
    "varianceDays": 16,
    "scheduleAdherence": "Behind Schedule",
    "schedulePerformance": "121.6%"
  },
  "budget": {
    "estimatedHours": 500.0,
    "actualHours": 550.0,
    "varianceHours": 50.0,
    "estimatedCost": 0,
    "actualCost": 0,
    "varianceCost": 0,
    "budgetAdherence": "Slightly Over",
    "costPerformance": "110.0%"
  },
  "phases": [...],
  "tickets": [...],
  "aiGeneratedSummary": "...",
  "generatedAt": "2024-10-29T04:00:00Z"
}
```

## Configuration

The API requires the following configuration settings:

### ConnectWise Configuration

- `ConnectWise:BaseUrl`: The base URL for your ConnectWise API (default: `https://na.myconnectwise.net/v4_6_release/apis/3.0`)
- `ConnectWise:CompanyId`: Your ConnectWise company identifier
- `ConnectWise:PublicKey`: Your ConnectWise API public key
- `ConnectWise:PrivateKey`: Your ConnectWise API private key
- `ConnectWise:ClientId`: Your ConnectWise client ID (application identifier)

### Azure OpenAI Configuration

- `AzureOpenAI:Endpoint`: Your Azure OpenAI resource endpoint (e.g., `https://your-resource.openai.azure.com/`)
- `AzureOpenAI:ApiKey`: Your Azure OpenAI API key
- `AzureOpenAI:DeploymentName`: The deployment name for your GPT model (default: `gpt-4`)

### Local Development

For local development, update the `local.settings.json` file with your credentials:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ConnectWise:BaseUrl": "https://na.myconnectwise.net/v4_6_release/apis/3.0",
    "ConnectWise:CompanyId": "your-company-id",
    "ConnectWise:PublicKey": "your-public-key",
    "ConnectWise:PrivateKey": "your-private-key",
    "ConnectWise:ClientId": "your-client-id",
    "AzureOpenAI:Endpoint": "https://your-resource-name.openai.azure.com/",
    "AzureOpenAI:ApiKey": "your-api-key",
    "AzureOpenAI:DeploymentName": "gpt-4"
  }
}
```

### Azure Deployment

When deploying to Azure Functions, configure these settings as Application Settings in the Azure Portal or via Azure CLI.

## Architecture

The API is built using:

- **Azure Functions** (Isolated Worker Model) for serverless hosting
- **.NET 9.0** runtime
- **ConnectWise Manage API** for project data retrieval
- **Azure OpenAI** for AI-powered report generation

### Key Components

1. **ProjectCompletionReportFunction**: The main HTTP-triggered Azure Function endpoint
2. **IProjectReportingService**: Orchestrates data gathering and report generation
3. **IConnectWiseApiClient**: HTTP client wrapper for ConnectWise API calls
4. **IAzureOpenAIService**: Azure OpenAI integration for generating summaries
5. **DTOs**: Data Transfer Objects for request/response models
6. **Models**: ConnectWise entity models

## Report Content

The project completion report includes:

1. **Project Summary**: Basic project information (status, dates, manager, company)
2. **Timeline Analysis**: 
   - Planned vs actual duration
   - Schedule variance
   - Schedule adherence assessment
3. **Budget Analysis**:
   - Planned vs actual hours
   - Hours variance
   - Budget adherence assessment
4. **Phase Details**: For each project phase:
   - Status and dates
   - Estimated vs actual hours
   - Associated notes
5. **Ticket Summaries**: For each ticket:
   - Ticket number and summary
   - Status, type, and subtype
   - Estimated vs actual hours
   - Associated notes
   - Assignment information
6. **AI-Generated Summary**: Comprehensive analysis including:
   - Overall project performance
   - Time budget adherence
   - Schedule adherence
   - Quality of notes vs actual completion
   - Key insights and recommendations

## Usage for Frontend

The JSON response is designed to be easily consumed by frontend applications (e.g., Blazor) for:

- Displaying project metrics
- Generating PDF reports
- Creating data visualizations
- Presenting AI-generated insights

The structured format allows frontend developers to:
- Bind directly to UI components
- Generate charts and graphs
- Create custom report layouts
- Export to various formats (PDF, Excel, etc.)

## Error Handling

The API returns appropriate HTTP status codes:

- `200 OK`: Successful report generation
- `400 Bad Request`: Invalid project ID
- `404 Not Found`: Project not found
- `500 Internal Server Error`: Server error during processing

## Security Considerations

- Store API keys and credentials securely (use Azure Key Vault in production)
- The `local.settings.json` file is excluded from source control
- Use Function-level authorization (API keys) for production deployments
- Consider implementing additional authentication/authorization as needed

## Development

To run locally:

1. Install Azure Functions Core Tools
2. Configure `local.settings.json` with valid credentials
3. Run `func start` or press F5 in Visual Studio

To test the endpoint:

```bash
curl -X POST http://localhost:7071/api/reports/project-completion \
  -H "Content-Type: application/json" \
  -d '{"projectId": 12345}'
```
