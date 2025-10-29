using Bezalu.ProjectReporting.API.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Azure.AI.OpenAI;
using Azure.Identity;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services.AddHttpClient<IConnectWiseApiClient, ConnectWiseApiClient>();

// Azure OpenAI via SDK + Managed Identity / DefaultAzureCredential
builder.Services.AddSingleton(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var endpoint = cfg["AzureOpenAI:Endpoint"] ?? cfg.GetSection("AzureOpenAI")["Endpoint"] ?? string.Empty;
    if (string.IsNullOrWhiteSpace(endpoint))
    {
        throw new InvalidOperationException("AzureOpenAI:Endpoint not configured.");
    }
    return new OpenAIClient(new Uri(endpoint), new DefaultAzureCredential());
});

builder.Services.AddScoped<IAzureOpenAIService, AzureOpenAIService>();
builder.Services.AddScoped<IProjectReportingService, ProjectReportingService>();

builder.Build().Run();
