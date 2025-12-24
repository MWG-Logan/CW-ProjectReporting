using Bezalu.ProjectReporting.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Bezalu.ProjectReporting.API.Functions;

public class ProjectsFunction(
    ILogger<ProjectsFunction> logger,
    IProjectReportingService reportingService)
{
    [Function("GetActiveProjects")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects")]
        HttpRequest req,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing get active projects request");

        try
        {
            var projects = await reportingService.GetActiveProjectsAsync(cancellationToken);
            return new OkObjectResult(projects);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving active projects");
            return new ObjectResult(new { error = "An error occurred while retrieving projects" })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}
