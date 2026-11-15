using WebOtchet.Models;
using WebOtchet.DTO.Incidents;

namespace WebOtchet.Repositories.IRepository;

public interface IIncidentRepository
{
    Task<Incident?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<Incident>> GetByPeriodAsync(
        DateTime from,
        DateTime to,
        int skip,
        int take,
        IncidentReportFilter? filter = null,
        CancellationToken cancellationToken = default
    );

    IAsyncEnumerable<Incident> StreamByPeriod(DateTime from, DateTime to, IncidentReportFilter filter);

    Task<int> CountByPeriodAsync(
        DateTime from,
        DateTime to,
        IncidentReportFilter? filter = null,
        CancellationToken cancellationToken = default
    );

    IAsyncEnumerable<IncidentReportValue> GetReportValues(DateTime from, DateTime to, IncidentReportFilter filter);

    Task AddAsync(
        Incident incident,
        CancellationToken cancellationToken = default
    );

    Task<bool> UpdateAsync(
        Incident incident,
        CancellationToken cancellationToken = default
    );

    Task<bool> CloseAsync(Incident incident, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );
}
