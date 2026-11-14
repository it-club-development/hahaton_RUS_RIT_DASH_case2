using System.ComponentModel.DataAnnotations;
namespace WebOtchet.DTO.Incidents;

public sealed class IncidentReportFilter
{
    public string? Direction { get; init; }
    public string? Center { get; init; }
    [RegularExpression("^(open|closed)$")]
    public string? Status { get; init; }
    [RegularExpression("^(asc|desc)$")]
    public string? Sort { get; init; }
}

public sealed record IncidentReportValue(string Direction, DateTime? EndTime, TimeSpan? Downtime);
public sealed record IncidentReportSummary(int Open, int Closed, int Skipped, IReadOnlyDictionary<string, long> DowntimeByDirection);
