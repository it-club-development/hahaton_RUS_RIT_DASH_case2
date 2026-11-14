using Microsoft.EntityFrameworkCore;
using WebOtchet.Data;
using WebOtchet.Models;
using WebOtchet.Repositories.IRepository;

namespace WebOtchet.Repositories.Repository;

public sealed class IncidentRepository : IIncidentRepository
{
    private readonly WebotchetDbContext _context;

    public IncidentRepository(WebotchetDbContext context)
    {
        _context = context;
    }

    private IQueryable<Incident> GetPeriodQuery(
        DateTime from,
        DateTime to)
    {
        if (to <= from)
            throw new ArgumentException(
                "Дата окончания периода должна быть позже даты начала."
            );

        return _context.Incidents
            .AsNoTracking()
            .Where(x =>
                x.BeginTime >= from &&
                x.BeginTime < to
            );
    }

    public async Task<Incident?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Incidents
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken
            );
    }

    public async Task<IReadOnlyList<Incident>> GetByPeriodAsync(
        DateTime from,
        DateTime to,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        if (skip < 0)
            throw new ArgumentOutOfRangeException(nameof(skip));

        if (take <= 0)
            throw new ArgumentOutOfRangeException(nameof(take));

        return await GetPeriodQuery(from, to)
            .OrderByDescending(x => x.BeginTime)
            .ThenBy(x => x.Id)
            .ThenBy(x => x.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByPeriodAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        return await GetPeriodQuery(from, to)
            .CountAsync(cancellationToken);
    }

    public async Task AddAsync(
        Incident incident,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(incident);

        if (incident.Id == Guid.Empty)
            incident.Id = Guid.NewGuid();

        await _context.Incidents.AddAsync(
            incident,
            cancellationToken
        );

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(
        Incident incident,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(incident);

        var existing = await _context.Incidents
            .FirstOrDefaultAsync(
                x => x.Id == incident.Id,
                cancellationToken
            );

        if (existing is null)
            return false;

        _context.Entry(existing)
            .CurrentValues
            .SetValues(incident);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var incident = await _context.Incidents
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken
            );

        if (incident is null)
            return false;

        _context.Incidents.Remove(incident);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
