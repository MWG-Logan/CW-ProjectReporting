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

public class ProjectReportingService(
    IConnectWiseApiClient connectWiseClient,
    IAzureOpenAIService aiService,
    ILogger<ProjectReportingService> logger)
    : IProjectReportingService
{
    public async Task<ProjectCompletionReportResponse> GenerateProjectCompletionReportAsync(
        int projectId, 
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Generating project completion report for project {ProjectId}", projectId);

        var project = await connectWiseClient.GetAsync<CWProject>(
            $"project/projects/{projectId}", 
            cancellationToken);

        if (project == null)
        {
            throw new InvalidOperationException($"Project {projectId} not found");
        }

        var projectNotes = await connectWiseClient.GetListAsync<CWProjectNote>(
            $"project/projects/{projectId}/notes",
            cancellationToken) ?? new List<CWProjectNote>();

        var tickets = await connectWiseClient.GetListAsync<CWTicket>(
            $"project/tickets?conditions=project/id={projectId}",
            cancellationToken) ?? new List<CWTicket>();

        var allTicketNotes = new Dictionary<int, List<CWTicketNote>>();
        foreach (var ticket in tickets)
        {
            if (ticket.Id.HasValue)
            {
                var ticketNotes = await connectWiseClient.GetListAsync<CWTicketNote>(
                    $"project/tickets/{ticket.Id.Value}/notes",
                    cancellationToken) ?? new List<CWTicketNote>();
                allTicketNotes[ticket.Id.Value] = ticketNotes;
            }
        }

        var phases = await connectWiseClient.GetListAsync<CWPhase>(
            $"project/projects/{projectId}/phases",
            cancellationToken) ?? new List<CWPhase>();

        var report = BuildReport(project, projectNotes, tickets, allTicketNotes, phases);

        var projectDataForAI = PrepareDataForAI(report, projectNotes, allTicketNotes);
        report.AiGeneratedSummary = await aiService.GenerateProjectSummaryAsync(
            projectDataForAI, 
            cancellationToken);

        report.GeneratedAt = DateTime.UtcNow;

        logger.LogInformation("Successfully generated project completion report for project {ProjectId}", projectId);
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

        var estimatedHours = project.EstimatedHours ?? 0;
        var actualHours = project.ActualHours ?? 0;
        var hoursVariance = actualHours - estimatedHours;

        report.Budget = new BudgetAnalysis
        {
            EstimatedHours = estimatedHours,
            ActualHours = actualHours,
            VarianceHours = hoursVariance,
            EstimatedCost = 0,
            ActualCost = 0,
            VarianceCost = 0,
            BudgetAdherence = hoursVariance <= 0 ? "Under Budget" : hoursVariance <= estimatedHours * 0.1m ? "Slightly Over" : "Over Budget",
            CostPerformance = estimatedHours > 0 ? $"{(double)(actualHours / estimatedHours) * 100:F1}%" : "N/A"
        };

        report.Phases = phases.Select(phase => new PhaseDetail
        {
            PhaseId = phase.Id ?? 0,
            PhaseName = phase.Description,
            Status = phase.Status?.Name,
            ActualStart = phase.ActualStart,
            ActualEnd = phase.ActualEnd,
            EstimatedHours = phase.EstimatedHours ?? 0,
            ActualHours = phase.ActualHours ?? 0,
            Notes = new List<string>()
        }).ToList();

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

    private static string Sanitize(string? text, int maxLen = 500)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var cleaned = text.Replace("\r", " ").Replace("\n", " ").Trim();
        return cleaned.Length <= maxLen ? cleaned : cleaned.Substring(0, maxLen) + "…";
    }

    private string PrepareDataForAI(ProjectCompletionReportResponse report, List<CWProjectNote> projectNotes, Dictionary<int, List<CWTicketNote>> ticketNotes)
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

        // Project notes content
        sb.AppendLine($"PROJECT NOTES ({projectNotes.Count}):");
        foreach (var note in projectNotes.OrderBy(n => n.DateCreated)) // cap to 50
        {
            sb.AppendLine($"- [{note.DateCreated:yyyy-MM-dd}] {Sanitize(note.Text)}");
        }
        sb.AppendLine();

        // Phase summaries
        sb.AppendLine($"PHASES ({report.Phases?.Count ?? 0}):");
        foreach (var phase in report.Phases ?? new List<PhaseDetail>())
        {
            sb.AppendLine($"- {phase.PhaseName}: {phase.Status}; Hours est/actual {phase.EstimatedHours}/{phase.ActualHours}");
        }
        sb.AppendLine();

        // Ticket + notes content (truncate per ticket)
        sb.AppendLine($"TICKETS ({report.Tickets?.Count ?? 0}):");
        foreach (var ticket in report.Tickets ?? new List<TicketSummary>())
        {
            sb.AppendLine($"- Ticket #{ticket.TicketNumber} {ticket.Summary} (Status: {ticket.Status}, Type: {ticket.Type}/{ticket.SubType}, Hours est/actual {ticket.EstimatedHours}/{ticket.ActualHours})");
            if (ticketNotes.TryGetValue(ticket.TicketId, out var notes) && notes.Any())
            {
                var limited = notes.OrderBy(n => n.DateCreated).ToList(); // cap to 20 per ticket
                sb.AppendLine("  Notes:");
                foreach (var n in limited)
                {
                    sb.AppendLine($"    • [{n.DateCreated:yyyy-MM-dd}] {Sanitize(n.Text)}");
                }
                if (notes.Count > limited.Count)
                {
                    sb.AppendLine($"    • … ({notes.Count - limited.Count} more notes truncated)");
                }
            }
        }

        return sb.ToString();
    }
}
