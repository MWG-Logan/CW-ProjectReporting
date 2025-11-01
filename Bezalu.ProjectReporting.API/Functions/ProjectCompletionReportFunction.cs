using Bezalu.ProjectReporting.API.Services;
using Bezalu.ProjectReporting.Shared.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

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

            if (request is not { ProjectId: > 0 })
                return new BadRequestObjectResult(new { error = "Invalid project ID" });

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

    // Changed to POST and accepts full report body to avoid regeneration
    [Function("GenerateProjectCompletionReportPdf")]
    public async Task<IActionResult> RunPdf(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "reports/project-completion/pdf")]
        HttpRequest req,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing project completion report PDF request (direct report body)");

        try
        {
            var report = await req.ReadFromJsonAsync<ProjectCompletionReportResponse>(cancellationToken);
            if (report is not { ProjectId: > 0 })
                return new BadRequestObjectResult(new { error = "Invalid report payload" });

            Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(stack =>
                        {
                            stack.Item().Text(report.ProjectName ?? "Project").FontSize(18).SemiBold();
                            stack.Item().Text($"Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm} UTC").FontSize(9);
                        });
                        row.ConstantItem(80).AlignRight().Column(stack =>
                        {
                            stack.Item().Text($"ID: {report.ProjectId}").FontSize(10);
                            stack.Item().Text(report.Summary?.Status ?? string.Empty).FontSize(10);
                        });
                    });

                    page.Content().Column(stack =>
                    {
                        if (report.Summary != null)
                            stack.Item().PaddingBottom(10).Element(SummarySection(report));
                        if (report.Timeline != null)
                            stack.Item().PaddingBottom(10).Element(TimelineSection(report));
                        if (report.Budget != null)
                            stack.Item().PaddingBottom(10).Element(BudgetSection(report));
                        if (!string.IsNullOrWhiteSpace(report.AiGeneratedSummary))
                            stack.Item().PaddingBottom(10).Element(AISummarySection(report));
                        if (report.Phases?.Any() == true)
                            stack.Item().PaddingBottom(10).Element(PhasesSection(report));
                        if (report.Tickets?.Any() == true)
                            stack.Item().PaddingBottom(10).Element(TicketsSection(report));
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Project Completion Report - Page ").FontSize(9);
                        text.CurrentPageNumber().FontSize(9);
                        text.Span(" / ").FontSize(9);
                        text.TotalPages().FontSize(9);
                    });
                });
            });

            var pdfBytes = document.GeneratePdf();
            return new FileContentResult(pdfBytes, "application/pdf")
            {
                FileDownloadName = $"project-{report.ProjectId}-completion-report.pdf"
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating project completion report PDF");
            return new ObjectResult(new { error = "An error occurred while generating the PDF" })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    private static Action<IContainer> SummarySection(ProjectCompletionReportResponse report)
    {
        return c =>
        {
            var s = report.Summary;
            c.Column(col =>
            {
                col.Item().Text("Project Summary").FontSize(14).Bold();
                col.Item().Text($"Manager: {s?.Manager ?? string.Empty}").FontSize(10);
                col.Item().Text($"Company: {s?.Company ?? string.Empty}").FontSize(10);
                col.Item().Text($"Status: {s?.Status ?? string.Empty}").FontSize(10);
                if (s is { PlannedStart: not null, PlannedEnd: not null })
                    col.Item().Text($"Planned: {s.PlannedStart:yyyy-MM-dd} > {s.PlannedEnd:yyyy-MM-dd}").FontSize(10);
                if (s is { ActualStart: not null, ActualEnd: not null })
                    col.Item().Text($"Actual: {s.ActualStart:yyyy-MM-dd} > {s.ActualEnd:yyyy-MM-dd}").FontSize(10);
            });
        };
    }

    private static Action<IContainer> TimelineSection(ProjectCompletionReportResponse report)
    {
        return c =>
        {
            var t = report.Timeline;
            c.Column(col =>
            {
                col.Item().Text("Timeline Analysis").FontSize(14).Bold();
                col.Item().Text($"Planned Days: {t?.PlannedDays}").FontSize(10);
                col.Item().Text($"Actual Days: {t?.TotalDays}").FontSize(10);
                col.Item().Text($"Variance: {t?.VarianceDays}").FontSize(10);
                col.Item().Text($"Schedule Adherence: {t?.ScheduleAdherence}").FontSize(10);
                col.Item().Text($"Schedule Performance: {t?.SchedulePerformance}").FontSize(10);
            });
        };
    }

    private static Action<IContainer> BudgetSection(ProjectCompletionReportResponse report)
    {
        return c =>
        {
            var b = report.Budget;
            c.Column(col =>
            {
                col.Item().Text("Budget Analysis").FontSize(14).Bold();
                col.Item().Text($"Estimated Hours: {b?.EstimatedHours}").FontSize(10);
                col.Item().Text($"Actual Hours: {b?.ActualHours}").FontSize(10);
                col.Item().Text($"Variance Hours: {b?.VarianceHours}").FontSize(10);
                col.Item().Text($"Budget Adherence: {b?.BudgetAdherence}").FontSize(10);
                col.Item().Text($"Cost Performance: {b?.CostPerformance}").FontSize(10);
            });
        };
    }

    private static Action<IContainer> AISummarySection(ProjectCompletionReportResponse report)
    {
        return c =>
        {
            c.Column(col =>
            {
                col.Item().Text("AI Generated Summary").FontSize(14).Bold();
                col.Item().Text(report.AiGeneratedSummary ?? string.Empty).FontSize(10);
            });
        };
    }

    private static Action<IContainer> PhasesSection(ProjectCompletionReportResponse report)
    {
        return c =>
        {
            c.Column(col =>
            {
                col.Item().Text("Phases").FontSize(14).Bold();
                foreach (var phase in report.Phases ?? [])
                    col.Item().BorderBottom(1).PaddingVertical(4).Column(inner =>
                    {
                        inner.Item().Text($"{phase.PhaseName} ({phase.Status})").SemiBold();
                        if (phase is { ActualStart: not null, ActualEnd: not null })
                            inner.Item().Text($"Actual: {phase.ActualStart:yyyy-MM-dd} > {phase.ActualEnd:yyyy-MM-dd}")
                                .FontSize(9);
                        inner.Item().Text($"Hours est/actual: {phase.EstimatedHours}/{phase.ActualHours}").FontSize(9);
                    });
            });
        };
    }

    private static Action<IContainer> TicketsSection(ProjectCompletionReportResponse report)
    {
        return c =>
        {
            c.Column(col =>
            {
                col.Item().Text("Tickets").FontSize(14).Bold();
                const int maxNotes = 10;
                foreach (var ticket in report.Tickets ?? [])
                    col.Item().BorderBottom(1).PaddingVertical(4).Column(inner =>
                    {
                        inner.Item().Text($"#{ticket.TicketNumber} {ticket.Summary} ({ticket.Status})").SemiBold();
                        inner.Item().Text($"Type: {ticket.Type}/{ticket.SubType}").FontSize(9);
                        inner.Item().Text($"Hours est/actual: {ticket.EstimatedHours}/{ticket.ActualHours}")
                            .FontSize(9);
                        if (ticket.ClosedDate != null)
                            inner.Item().Text($"Closed: {ticket.ClosedDate:yyyy-MM-dd}").FontSize(9);
                        if (!string.IsNullOrWhiteSpace(ticket.AssignedTo))
                            inner.Item().Text($"Assigned: {ticket.AssignedTo}").FontSize(9);
                        if (ticket.Notes?.Any() != true) return;
                        inner.Item().Text("Notes:").FontSize(9);
                        foreach (var n in ticket.Notes.Take(maxNotes))
                            inner.Item().Text($" - {n}").FontSize(9);
                        if (ticket.Notes.Count > maxNotes)
                            inner.Item().Text($" - ... ({ticket.Notes.Count - maxNotes} more notes truncated)").FontSize(9);
                    });
            });
        };
    }
}