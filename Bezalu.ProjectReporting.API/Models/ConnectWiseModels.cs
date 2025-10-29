namespace Bezalu.ProjectReporting.API.Models;

public class CWProject
{
    public int? Id { get; set; }
    public string? Name { get; set; }
    public CWReference? Status { get; set; }
    public DateTime? ActualStart { get; set; }
    public DateTime? ActualEnd { get; set; }
    public DateTime? EstimatedStart { get; set; }
    public DateTime? EstimatedEnd { get; set; }
    public decimal? EstimatedHours { get; set; }
    public decimal? ActualHours { get; set; }
    public CWReference? Manager { get; set; }
    public CWReference? Company { get; set; }
}

public class CWProjectNote
{
    public int? Id { get; set; }
    public string? Text { get; set; }
    public DateTime? DateCreated { get; set; }
    public CWReference? CreatedBy { get; set; }
}

public class CWTicket
{
    public int? Id { get; set; }
    public string? TicketNumber { get; set; }
    public string? Summary { get; set; }
    public CWReference? Status { get; set; }
    public CWReference? Type { get; set; }
    public CWReference? SubType { get; set; }
    public decimal? EstimatedHours { get; set; }
    public decimal? ActualHours { get; set; }
    public DateTime? ClosedDate { get; set; }
    public CWReference? AssignedTo { get; set; }
}

public class CWTicketNote
{
    public int? Id { get; set; }
    public string? Text { get; set; }
    public DateTime? DateCreated { get; set; }
    public CWReference? CreatedBy { get; set; }
}

public class CWPhase
{
    public int? Id { get; set; }
    public string? Description { get; set; }
    public CWReference? Status { get; set; }
    public DateTime? ActualStart { get; set; }
    public DateTime? ActualEnd { get; set; }
    public decimal? EstimatedHours { get; set; }
    public decimal? ActualHours { get; set; }
}

public class CWPhaseNote
{
    public int? Id { get; set; }
    public string? Text { get; set; }
    public DateTime? DateCreated { get; set; }
}

public class CWReference
{
    public int? Id { get; set; }
    public string? Name { get; set; }
}
