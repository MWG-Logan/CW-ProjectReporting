# Configuration Guide

This guide explains how to configure the Bezalu.ProjectReporting API for your environment.

## Prerequisites

Before configuring, ensure you have:

1. **ConnectWise Manage Access**
   - Company ID
   - API Member with public/private key pair
   - Client ID (from registered application)

2. **Azure OpenAI Resource**
   - Azure subscription
   - Azure OpenAI resource created
   - GPT-4 or GPT-3.5-turbo deployment
   - API key

## Step 1: ConnectWise Manage API Setup

### Create API Member

1. Log in to ConnectWise Manage as an administrator
2. Navigate to **System > Members**
3. Create a new API Member:
   - Set member type to "API Member"
   - Generate API keys (public and private)
   - Save the keys securely (private key is only shown once)
4. Assign appropriate security roles for:
   - Reading projects
   - Reading project notes
   - Reading project tickets
   - Reading project phases

### Register Application

1. Navigate to **System > API > Members**
2. Register your application to get a Client ID
3. Note down the Client ID

### Find Your API Base URL

Your ConnectWise API base URL typically follows this format:
```
https://[region].myconnectwise.net/v4_6_release/apis/3.0
```

Common regions:
- `na` - North America
- `eu` - Europe
- `au` - Australia
- `staging` - Staging environment

## Step 2: Azure OpenAI Setup

### Create Azure OpenAI Resource

1. Log in to Azure Portal
2. Create a new "Azure OpenAI" resource:
   - Choose your subscription
   - Create or select a resource group
   - Choose a region
   - Set the pricing tier
3. Once deployed, go to the resource

### Deploy a Model

1. In your Azure OpenAI resource, go to **Model deployments**
2. Click **Create new deployment**
3. Select a model (recommended: `gpt-4` or `gpt-35-turbo`)
4. Give it a deployment name (e.g., "gpt-4")
5. Deploy the model

### Get Your Credentials

1. In your Azure OpenAI resource, go to **Keys and Endpoint**
2. Copy:
   - Endpoint URL (e.g., `https://your-resource.openai.azure.com/`)
   - Key 1 or Key 2

## Step 3: Local Development Configuration

### Update local.settings.json

1. Navigate to `Bezalu.ProjectReporting.API`
2. Open `local.settings.json`
3. Update the following values:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    
    "ConnectWise:BaseUrl": "https://na.myconnectwise.net/v4_6_release/apis/3.0",
    "ConnectWise:CompanyId": "your_company_id",
    "ConnectWise:PublicKey": "your_public_key",
    "ConnectWise:PrivateKey": "your_private_key",
    "ConnectWise:ClientId": "your_client_id",
    
    "AzureOpenAI:Endpoint": "https://your-resource-name.openai.azure.com/",
    "AzureOpenAI:ApiKey": "your_api_key",
    "AzureOpenAI:DeploymentName": "gpt-4"
  }
}
```

### Configuration Details

#### ConnectWise Settings

| Setting | Description | Example |
|---------|-------------|---------|
| `BaseUrl` | Your ConnectWise API base URL | `https://na.myconnectwise.net/v4_6_release/apis/3.0` |
| `CompanyId` | Your company identifier in ConnectWise | `companyname` |
| `PublicKey` | API Member public key | `ABC123...` |
| `PrivateKey` | API Member private key | `XYZ789...` |
| `ClientId` | Registered application client ID | `12345678-1234-1234-1234-123456789012` |

#### Azure OpenAI Settings

| Setting | Description | Example |
|---------|-------------|---------|
| `Endpoint` | Azure OpenAI resource endpoint | `https://my-openai.openai.azure.com/` |
| `ApiKey` | Azure OpenAI API key | `abcdef123456...` |
| `DeploymentName` | Name of your GPT deployment | `gpt-4` |

## Step 4: Azure Deployment Configuration

When deploying to Azure Functions, configure application settings:

### Via Azure Portal

1. Navigate to your Function App
2. Go to **Configuration** > **Application settings**
3. Add each setting from `local.settings.json` as a new application setting
4. Click **Save**

### Via Azure CLI

```bash
# Set ConnectWise settings
az functionapp config appsettings set --name <function-app-name> \
  --resource-group <resource-group> \
  --settings \
  "ConnectWise__BaseUrl=https://na.myconnectwise.net/v4_6_release/apis/3.0" \
  "ConnectWise__CompanyId=your_company_id" \
  "ConnectWise__PublicKey=your_public_key" \
  "ConnectWise__PrivateKey=your_private_key" \
  "ConnectWise__ClientId=your_client_id"

# Set Azure OpenAI settings
az functionapp config appsettings set --name <function-app-name> \
  --resource-group <resource-group> \
  --settings \
  "AzureOpenAI__Endpoint=https://your-resource.openai.azure.com/" \
  "AzureOpenAI__ApiKey=your_api_key" \
  "AzureOpenAI__DeploymentName=gpt-4"
```

**Note:** In Azure configuration, use double underscores (`__`) instead of colons (`:`) in setting names.

## Step 5: Security Best Practices

### Use Azure Key Vault (Recommended for Production)

1. Create an Azure Key Vault
2. Store secrets in Key Vault:
   - `ConnectWise-PrivateKey`
   - `AzureOpenAI-ApiKey`
3. Enable managed identity for your Function App
4. Grant the Function App access to Key Vault
5. Reference secrets in application settings:
   ```
   @Microsoft.KeyVault(SecretUri=https://your-vault.vault.azure.net/secrets/ConnectWise-PrivateKey/)
   ```

### Local Development

- Never commit `local.settings.json` (already in `.gitignore`)
- Use different API keys for development and production
- Restrict API member permissions to minimum required

## Step 6: Testing Configuration

### Test ConnectWise Connection

```bash
# Replace with your actual endpoint and credentials
curl -X GET "https://na.myconnectwise.net/v4_6_release/apis/3.0/project/projects?pageSize=1" \
  -H "Authorization: Basic $(echo -n 'companyId+publicKey:privateKey' | base64)" \
  -H "clientId: your-client-id" \
  -H "Accept: application/json"
```

### Test Azure OpenAI Connection

```bash
curl -X POST "https://your-resource.openai.azure.com/openai/deployments/gpt-4/chat/completions?api-version=2024-02-15-preview" \
  -H "Content-Type: application/json" \
  -H "api-key: your-api-key" \
  -d '{
    "messages": [{"role": "user", "content": "Hello"}],
    "max_tokens": 10
  }'
```

### Test API Endpoint

1. Start the Functions app locally:
   ```bash
   cd Bezalu.ProjectReporting.API
   func start
   ```

2. Test the endpoint:
   ```bash
   curl -X POST http://localhost:7071/api/reports/project-completion \
     -H "Content-Type: application/json" \
     -d '{"projectId": <valid-project-id>}'
   ```

## Troubleshooting

### Common Issues

**"Unauthorized" from ConnectWise API**
- Verify your company ID, public key, and private key
- Ensure the API member has proper security roles
- Check that the client ID is correct

**"401 Unauthorized" from Azure OpenAI**
- Verify your API key is correct
- Ensure the endpoint URL is correct and includes the trailing slash
- Check that your Azure OpenAI resource is not in a restricted region

**"404 Not Found" on API endpoint**
- Ensure the Azure Functions runtime is running
- Check the route in `ProjectCompletionReportFunction.cs`
- Verify FUNCTIONS_WORKER_RUNTIME is set to "dotnet-isolated"

**"Project not found" error**
- Verify the project ID exists in ConnectWise
- Ensure the API member has permission to view the project

## Support

For issues or questions:
1. Check the [API README](./Bezalu.ProjectReporting.API/README.md)
2. Review Azure Functions logs
3. Check ConnectWise API documentation: https://developer.connectwise.com/
4. Check Azure OpenAI documentation: https://learn.microsoft.com/azure/ai-services/openai/
