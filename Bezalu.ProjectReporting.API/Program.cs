using Bezalu.ProjectReporting.API.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Azure.AI.OpenAI;
using Azure.Identity;
using OpenAI.Chat;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services.AddHttpClient<IConnectWiseApiClient, ConnectWiseApiClient>();

// Azure OpenAI v2.1.0
builder.Services.AddSingleton<ChatClient>(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var endpoint = cfg["AzureOpenAI:Endpoint"] ?? string.Empty;
    var deployment = cfg["AzureOpenAI:DeploymentName"] ?? "gpt-4";
    if (string.IsNullOrWhiteSpace(endpoint))
    {
        throw new InvalidOperationException("AzureOpenAI:Endpoint not configured.");
    }
    var client = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential());
    return client.GetChatClient(deployment);
});

builder.Services.AddScoped<IAzureOpenAIService, AzureOpenAIService>();
builder.Services.AddScoped<IProjectReportingService, ProjectReportingService>();

builder.Build().Run();
