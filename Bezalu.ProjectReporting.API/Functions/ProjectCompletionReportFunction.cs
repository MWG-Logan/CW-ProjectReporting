using Bezalu.ProjectReporting.API.DTOs;
using Bezalu.ProjectReporting.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Bezalu.ProjectReporting.API.Functions;

public class ProjectCompletionReportFunction(
    ILogger<ProjectCompletionReportFunction> logger,
    IProjectReportingService reportingService)
{
    [Function("GenerateProjectCompletionReport")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "reports/project-completion")] 
        HttpRequest req,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing project completion report request");

        try
        {
            var request = await req.ReadFromJsonAsync<ProjectCompletionReportRequest>(cancellationToken);
            
            if (request == null || request.ProjectId <= 0)
            {
                return new BadRequestObjectResult(new { error = "Invalid project ID" });
            }

            var report = await reportingService.GenerateProjectCompletionReportAsync(
                request.ProjectId, 
                cancellationToken);

            return new OkObjectResult(report);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Project not found");
            return new NotFoundObjectResult(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating project completion report");
            return new ObjectResult(new { error = "An error occurred while generating the report" })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}
