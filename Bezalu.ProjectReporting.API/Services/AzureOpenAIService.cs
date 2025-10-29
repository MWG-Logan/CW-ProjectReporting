using System.Linq;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace Bezalu.ProjectReporting.API.Services;

public interface IAzureOpenAIService
{
    Task<string> GenerateProjectSummaryAsync(string projectData, CancellationToken cancellationToken = default);
}

public class AzureOpenAIService(ChatClient chatClient, ILogger<AzureOpenAIService> logger)
    : IAzureOpenAIService
{
    public async Task<string> GenerateProjectSummaryAsync(string projectData, CancellationToken cancellationToken = default)
    {
        try
        {
            const string systemPrompt = @"You are an expert project analyst. Analyze the provided project data and generate a comprehensive completion report that includes:
1. Summary of ticket/phase actions and completion.
2. Time budget adherence analysis (planned vs actual hours).
3. Schedule adherence analysis (planned vs actual dates)
4. Notes quality compared to actual task completion.
5. Overall project performance assessment.
6. Key insights and recommendations.

Format your response in clear, structured sections suitable for a professional project report.";

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage($"Analyze this project data and provide a comprehensive completion report:\n\n{projectData}")
            };

            var options = new ChatCompletionOptions
            {
                Temperature = 0.7f,
                MaxOutputTokenCount = 2000
            };

            var result = await chatClient.CompleteChatAsync(messages, options, cancellationToken);
            var completion = result.Value;
            var content = completion.Content.FirstOrDefault()?.Text;
            return string.IsNullOrWhiteSpace(content) ? "Unable to generate summary." : content.Trim();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating project summary with Azure OpenAI");
            return "Error generating AI summary. Manual review required.";
        }
    }
}
