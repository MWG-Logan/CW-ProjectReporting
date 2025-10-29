using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Bezalu.ProjectReporting.API.Services;

public interface IAzureOpenAIService
{
    Task<string> GenerateProjectSummaryAsync(string projectData, CancellationToken cancellationToken = default);
}

public class AzureOpenAIService : IAzureOpenAIService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AzureOpenAIService> _logger;
    private readonly string _deploymentName;

    public AzureOpenAIService(HttpClient httpClient, ILogger<AzureOpenAIService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _deploymentName = configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4";
    }

    public async Task<string> GenerateProjectSummaryAsync(string projectData, CancellationToken cancellationToken = default)
    {
        try
        {
            var systemPrompt = @"You are an expert project analyst. Analyze the provided project data and generate a comprehensive completion report that includes:
1. Summary of ticket/phase actions and completion
2. Time budget adherence analysis (planned vs actual hours)
3. Schedule adherence analysis (planned vs actual dates)
4. Notes quality compared to actual task completion
5. Overall project performance assessment
6. Key insights and recommendations

Format your response in clear, structured sections suitable for a professional project report.";

            var requestBody = new
            {
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = $"Analyze this project data and provide a comprehensive completion report:\n\n{projectData}" }
                },
                max_tokens = 2000,
                temperature = 0.7
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                $"openai/deployments/{_deploymentName}/chat/completions?api-version=2024-02-15-preview",
                content,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var jsonDoc = JsonDocument.Parse(responseContent);
            
            var message = jsonDoc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return message ?? "Unable to generate summary.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating project summary with Azure OpenAI");
            return "Error generating AI summary. Manual review required.";
        }
    }
}
