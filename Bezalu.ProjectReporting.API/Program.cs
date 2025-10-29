using Bezalu.ProjectReporting.API.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Register HttpClient for ConnectWise API
builder.Services.AddHttpClient<IConnectWiseApiClient, ConnectWiseApiClient>((serviceProvider, client) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["ConnectWise:BaseUrl"] ?? "https://na.myconnectwise.net/v4_6_release/apis/3.0";
    var companyId = configuration["ConnectWise:CompanyId"] ?? "";
    var publicKey = configuration["ConnectWise:PublicKey"] ?? "";
    var privateKey = configuration["ConnectWise:PrivateKey"] ?? "";
    var clientId = configuration["ConnectWise:ClientId"] ?? "";

    client.BaseAddress = new Uri(baseUrl);
    
    // Set up ConnectWise authentication
    var authString = $"{companyId}+{publicKey}:{privateKey}";
    var base64Auth = Convert.ToBase64String(Encoding.ASCII.GetBytes(authString));
    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64Auth);
    client.DefaultRequestHeaders.Add("clientId", clientId);
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
});

// Register HttpClient for Azure OpenAI
builder.Services.AddHttpClient<IAzureOpenAIService, AzureOpenAIService>((serviceProvider, client) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var endpoint = configuration["AzureOpenAI:Endpoint"] ?? "";
    var apiKey = configuration["AzureOpenAI:ApiKey"] ?? "";

    if (!string.IsNullOrEmpty(endpoint))
    {
        client.BaseAddress = new Uri(endpoint);
    }
    client.DefaultRequestHeaders.Add("api-key", apiKey);
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
});

// Register application services
builder.Services.AddScoped<IProjectReportingService, ProjectReportingService>();

builder.Build().Run();
