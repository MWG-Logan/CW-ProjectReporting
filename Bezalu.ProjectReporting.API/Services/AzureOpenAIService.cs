using System.Text;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Bezalu.ProjectReporting.API.Services;

public interface IAzureOpenAIService
{
    Task<string> GenerateProjectSummaryAsync(string projectData, CancellationToken cancellationToken = default);
}

public class AzureOpenAIService(OpenAIClient openAiClient, ILogger<AzureOpenAIService> logger, IConfiguration configuration)
    : IAzureOpenAIService
{
    private readonly string _deploymentName = configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4";

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

            var messages = new List<ChatRequestMessage>
            {
                new ChatRequestSystemMessage(systemPrompt),
                new ChatRequestUserMessage($"Analyze this project data and provide a comprehensive completion report:\n\n{projectData}")
            };

            var options = new ChatCompletionsOptions(_deploymentName, messages)
            {
                Temperature = 0.7f,
                MaxTokens = 2000
            };
            foreach (var m in messages) options.Messages.Add(m);

            var response = await openAiClient.GetChatCompletionsAsync(options, cancellationToken);
            var content = response.Value.Choices.FirstOrDefault()?.Message.Content;
            return string.IsNullOrWhiteSpace(content) ? "Unable to generate summary." : content.Trim();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating project summary with Azure OpenAI");
            return "Error generating AI summary. Manual review required.";
        }
    }
}
