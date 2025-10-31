namespace Bezalu.ProjectReporting.Shared.DTOs;

public class ProjectCompletionReportRequest
{
 public int ProjectId { get; set; }
}

public class ProjectCompletionReportResponse
{
 public int ProjectId { get; set; }
 public string? ProjectName { get; set; }
 public ProjectSummary? Summary { get; set; }
 public TimelineAnalysis? Timeline { get; set; }
 public BudgetAnalysis? Budget { get; set; }
 public List<PhaseDetail>? Phases { get; set; }
 public List<TicketSummary>? Tickets { get; set; }
 public string? AiGeneratedSummary { get; set; }
 public DateTime GeneratedAt { get; set; }
}

public class ProjectSummary
{
 public string? Status { get; set; }
 public DateTime? ActualStart { get; set; }
 public DateTime? ActualEnd { get; set; }
 public DateTime? PlannedStart { get; set; }
 public DateTime? PlannedEnd { get; set; }
 public string? Manager { get; set; }
 public string? Company { get; set; }
}

public class TimelineAnalysis
{
 public int TotalDays { get; set; }
 public int PlannedDays { get; set; }
 public int VarianceDays { get; set; }
 public string? ScheduleAdherence { get; set; }
 public string? SchedulePerformance { get; set; }
}

public class BudgetAnalysis
{
 public decimal EstimatedHours { get; set; }
 public decimal ActualHours { get; set; }
 public decimal VarianceHours { get; set; }
 public decimal EstimatedCost { get; set; }
 public decimal ActualCost { get; set; }
 public decimal VarianceCost { get; set; }
 public string? BudgetAdherence { get; set; }
 public string? CostPerformance { get; set; }
}

public class PhaseDetail
{
 public int PhaseId { get; set; }
 public string? PhaseName { get; set; }
 public string? Status { get; set; }
 public DateTime? ActualStart { get; set; }
 public DateTime? ActualEnd { get; set; }
 public decimal EstimatedHours { get; set; }
 public decimal ActualHours { get; set; }
 public List<string>? Notes { get; set; }
 public string? Summary { get; set; }
}

public class TicketSummary
{
 public int TicketId { get; set; }
 public string? TicketNumber { get; set; }
 public string? Summary { get; set; }
 public string? Status { get; set; }
 public string? Type { get; set; }
 public string? SubType { get; set; }
 public decimal EstimatedHours { get; set; }
 public decimal ActualHours { get; set; }
 public List<string>? Notes { get; set; }
 public DateTime? ClosedDate { get; set; }
 public string? AssignedTo { get; set; }
}
