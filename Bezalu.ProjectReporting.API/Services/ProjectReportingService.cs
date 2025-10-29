using Bezalu.ProjectReporting.API.DTOs;
using Bezalu.ProjectReporting.API.Models;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace Bezalu.ProjectReporting.API.Services;

public interface IProjectReportingService
{
    Task<ProjectCompletionReportResponse> GenerateProjectCompletionReportAsync(int projectId, CancellationToken cancellationToken = default);
}

public class ProjectReportingService : IProjectReportingService
{
    private readonly IConnectWiseApiClient _connectWiseClient;
    private readonly IAzureOpenAIService _aiService;
    private readonly ILogger<ProjectReportingService> _logger;

    public ProjectReportingService(
        IConnectWiseApiClient connectWiseClient,
        IAzureOpenAIService aiService,
        ILogger<ProjectReportingService> logger)
    {
        _connectWiseClient = connectWiseClient;
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<ProjectCompletionReportResponse> GenerateProjectCompletionReportAsync(
        int projectId, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating project completion report for project {ProjectId}", projectId);

        // Fetch project details
        var project = await _connectWiseClient.GetAsync<CWProject>(
            $"project/projects/{projectId}", 
            cancellationToken);

        if (project == null)
        {
            throw new InvalidOperationException($"Project {projectId} not found");
        }

        // Fetch project notes
        var projectNotes = await _connectWiseClient.GetListAsync<CWProjectNote>(
            $"project/projects/{projectId}/notes",
            cancellationToken) ?? new List<CWProjectNote>();

        // Fetch project tickets
        var tickets = await _connectWiseClient.GetListAsync<CWTicket>(
            $"project/tickets?conditions=project/id={projectId}",
            cancellationToken) ?? new List<CWTicket>();

        // Fetch ticket notes for all tickets
        var allTicketNotes = new Dictionary<int, List<CWTicketNote>>();
        foreach (var ticket in tickets)
        {
            if (ticket.Id.HasValue)
            {
                var ticketNotes = await _connectWiseClient.GetListAsync<CWTicketNote>(
                    $"project/tickets/{ticket.Id.Value}/notes",
                    cancellationToken) ?? new List<CWTicketNote>();
                allTicketNotes[ticket.Id.Value] = ticketNotes;
            }
        }

        // Fetch project phases
        var phases = await _connectWiseClient.GetListAsync<CWPhase>(
            $"project/projects/{projectId}/phases",
            cancellationToken) ?? new List<CWPhase>();

        // Build the report
        var report = BuildReport(project, projectNotes, tickets, allTicketNotes, phases);

        // Generate AI summary
        var projectDataForAI = PrepareDataForAI(report);
        report.AiGeneratedSummary = await _aiService.GenerateProjectSummaryAsync(
            projectDataForAI, 
            cancellationToken);

        report.GeneratedAt = DateTime.UtcNow;

        _logger.LogInformation("Successfully generated project completion report for project {ProjectId}", projectId);
        return report;
    }

    private ProjectCompletionReportResponse BuildReport(
        CWProject project,
        List<CWProjectNote> projectNotes,
        List<CWTicket> tickets,
        Dictionary<int, List<CWTicketNote>> ticketNotes,
        List<CWPhase> phases)
    {
        var report = new ProjectCompletionReportResponse
        {
            ProjectId = project.Id ?? 0,
            ProjectName = project.Name,
            Summary = new ProjectSummary
            {
                Status = project.Status?.Name,
                ActualStart = project.ActualStart,
                ActualEnd = project.ActualEnd,
                PlannedStart = project.EstimatedStart,
                PlannedEnd = project.EstimatedEnd,
                Manager = project.Manager?.Name,
                Company = project.Company?.Name
            }
        };

        // Calculate timeline analysis
        if (project.EstimatedStart.HasValue && project.EstimatedEnd.HasValue &&
            project.ActualStart.HasValue)
        {
            var plannedDays = (project.EstimatedEnd.Value - project.EstimatedStart.Value).Days;
            var actualEnd = project.ActualEnd ?? DateTime.Now;
            var actualDays = (actualEnd - project.ActualStart.Value).Days;
            var variance = actualDays - plannedDays;

            report.Timeline = new TimelineAnalysis
            {
                TotalDays = actualDays,
                PlannedDays = plannedDays,
                VarianceDays = variance,
                ScheduleAdherence = variance <= 0 ? "On Time" : variance <= plannedDays * 0.1 ? "Slightly Behind" : "Behind Schedule",
                SchedulePerformance = $"{(plannedDays > 0 ? (double)actualDays / plannedDays * 100 : 100):F1}%"
            };
        }

        // Calculate budget analysis
        var estimatedHours = project.EstimatedHours ?? 0;
        var actualHours = project.ActualHours ?? 0;
        var hoursVariance = actualHours - estimatedHours;

        report.Budget = new BudgetAnalysis
        {
            EstimatedHours = estimatedHours,
            ActualHours = actualHours,
            VarianceHours = hoursVariance,
            EstimatedCost = 0, // Would need rate information
            ActualCost = 0,
            VarianceCost = 0,
            BudgetAdherence = hoursVariance <= 0 ? "Under Budget" : hoursVariance <= estimatedHours * 0.1m ? "Slightly Over" : "Over Budget",
            CostPerformance = estimatedHours > 0 ? $"{(double)(actualHours / estimatedHours) * 100:F1}%" : "N/A"
        };

        // Build phase details
        report.Phases = phases.Select(phase => new PhaseDetail
        {
            PhaseId = phase.Id ?? 0,
            PhaseName = phase.Description,
            Status = phase.Status?.Name,
            ActualStart = phase.ActualStart,
            ActualEnd = phase.ActualEnd,
            EstimatedHours = phase.EstimatedHours ?? 0,
            ActualHours = phase.ActualHours ?? 0,
            Notes = phase.Notes?.OrderBy(n => n.DateCreated).Select(n => n.Text ?? "").ToList() ?? new List<string>()
        }).ToList();

        // Build ticket summaries
        report.Tickets = tickets.Select(ticket => new TicketSummary
        {
            TicketId = ticket.Id ?? 0,
            TicketNumber = ticket.TicketNumber,
            Summary = ticket.Summary,
            Status = ticket.Status?.Name,
            Type = ticket.Type?.Name,
            SubType = ticket.SubType?.Name,
            EstimatedHours = ticket.EstimatedHours ?? 0,
            ActualHours = ticket.ActualHours ?? 0,
            Notes = ticketNotes.TryGetValue(ticket.Id ?? 0, out var notes)
                ? notes.OrderBy(n => n.DateCreated).Select(n => n.Text ?? "").ToList()
                : new List<string>(),
            ClosedDate = ticket.ClosedDate,
            AssignedTo = ticket.AssignedTo?.Name
        }).ToList();

        return report;
    }

    private string PrepareDataForAI(ProjectCompletionReportResponse report)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"PROJECT: {report.ProjectName}");
        sb.AppendLine($"Status: {report.Summary?.Status}");
        sb.AppendLine();

        sb.AppendLine("TIMELINE:");
        if (report.Timeline != null)
        {
            sb.AppendLine($"- Planned: {report.Timeline.PlannedDays} days");
            sb.AppendLine($"- Actual: {report.Timeline.TotalDays} days");
            sb.AppendLine($"- Variance: {report.Timeline.VarianceDays} days ({report.Timeline.ScheduleAdherence})");
        }
        sb.AppendLine();

        sb.AppendLine("BUDGET:");
        if (report.Budget != null)
        {
            sb.AppendLine($"- Estimated: {report.Budget.EstimatedHours} hours");
            sb.AppendLine($"- Actual: {report.Budget.ActualHours} hours");
            sb.AppendLine($"- Variance: {report.Budget.VarianceHours} hours ({report.Budget.BudgetAdherence})");
        }
        sb.AppendLine();

        sb.AppendLine($"PHASES ({report.Phases?.Count ?? 0}):");
        foreach (var phase in report.Phases ?? new List<PhaseDetail>())
        {
            sb.AppendLine($"- {phase.PhaseName}: {phase.Status}");
            sb.AppendLine($"  Hours: {phase.EstimatedHours} est / {phase.ActualHours} actual");
            if (phase.Notes?.Any() == true)
            {
                sb.AppendLine($"  Notes: {phase.Notes.Count} notes recorded");
            }
        }
        sb.AppendLine();

        sb.AppendLine($"TICKETS ({report.Tickets?.Count ?? 0}):");
        foreach (var ticket in report.Tickets ?? new List<TicketSummary>())
        {
            sb.AppendLine($"- #{ticket.TicketNumber}: {ticket.Summary}");
            sb.AppendLine($"  Status: {ticket.Status}, Type: {ticket.Type}/{ticket.SubType}");
            sb.AppendLine($"  Hours: {ticket.EstimatedHours} est / {ticket.ActualHours} actual");
            if (ticket.Notes?.Any() == true)
            {
                sb.AppendLine($"  Notes: {ticket.Notes.Count} notes recorded");
            }
        }

        return sb.ToString();
    }
}
