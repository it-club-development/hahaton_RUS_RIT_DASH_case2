using WebOtchet.Models;

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
        CancellationToken cancellationToken = default
    );

    Task<int> CountByPeriodAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default
    );

    Task AddAsync(
        Incident incident,
        CancellationToken cancellationToken = default
    );

    Task<bool> UpdateAsync(
        Incident incident,
        CancellationToken cancellationToken = default
    );

    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );
}
