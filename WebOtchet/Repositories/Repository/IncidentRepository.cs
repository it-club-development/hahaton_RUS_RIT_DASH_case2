using Microsoft.EntityFrameworkCore;
using WebOtchet.Data;
using WebOtchet.Infrastructure;
using WebOtchet.Models;
using WebOtchet.DTO.Incidents;
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
        DateTime to, IncidentReportFilter? filter)
    {
        if (to <= from)
            throw new ArgumentException(
                "Дата окончания периода должна быть позже даты начала."
            );

        var source = filter?.Status switch
        {
            "open" => _context.Incidents.FromSqlRaw("SELECT * FROM INCIDENTS WHERE END_TIME IS NULL OR END_TIME = '0000-00-00 00:00:00'"),
            "closed" => _context.Incidents.FromSqlRaw("SELECT * FROM INCIDENTS WHERE END_TIME IS NOT NULL AND END_TIME <> '0000-00-00 00:00:00'"),
            _ => _context.Incidents,
        };
        var query = source.AsNoTracking().Where(row => row.BeginTime >= from && row.BeginTime < to);
        if (!string.IsNullOrEmpty(filter?.Direction))
            query = query.Where(row => row.Direction == filter.Direction);
        if (!string.IsNullOrEmpty(filter?.Center))
            query = query.Where(row => row.TechCenter == filter.Center);
        return query;
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
        IncidentReportFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        if (skip < 0)
            throw new ArgumentOutOfRangeException(nameof(skip));

        if (take <= 0)
            throw new ArgumentOutOfRangeException(nameof(take));

        return await OrderByStart(GetPeriodQuery(from, to, filter), filter)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public IAsyncEnumerable<Incident> StreamByPeriod(DateTime from, DateTime to, IncidentReportFilter filter) =>
        OrderByStart(GetPeriodQuery(from, to, filter), filter).AsAsyncEnumerable();

    private static IOrderedQueryable<Incident> OrderByStart(IQueryable<Incident> query, IncidentReportFilter? filter) =>
        (filter?.Sort == "asc" ? query.OrderBy(x => x.BeginTime) : query.OrderByDescending(x => x.BeginTime))
        .ThenBy(x => x.Id);

    public async Task<int> CountByPeriodAsync(
        DateTime from,
        DateTime to,
        IncidentReportFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        return await GetPeriodQuery(from, to, filter)
            .CountAsync(cancellationToken);
    }

    public IAsyncEnumerable<IncidentReportValue> GetReportValues(DateTime from, DateTime to, IncidentReportFilter filter) =>
        GetPeriodQuery(from, to, filter)
            .Select(row => new IncidentReportValue(row.Direction, row.EndTime, row.CommonStationTime))
            .AsAsyncEnumerable();

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

        if (existing.Version != incident.Version)
            throw new WriteConflictException("Авария изменилась. Откройте её заново перед сохранением.");

        incident.CreatedByUserId = existing.CreatedByUserId;
        incident.Version = Guid.NewGuid();
        _context.Entry(existing).CurrentValues.SetValues(incident);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new WriteConflictException("Авария изменилась. Откройте её заново перед сохранением.");
        }

        return true;
    }

    public async Task<bool> CloseAsync(Incident incident, CancellationToken cancellationToken = default)
    {
        var id = incident.Id.ToString("D");
        var version = Guid.NewGuid().ToString("D");
        var expectedVersion = incident.Version.ToString("D");
        var updated = await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE INCIDENTS SET END_TIME = {incident.EndTime}, LENGHT = {incident.Length}, ROW_VERSION = {version}
            WHERE ID = {id} AND ROW_VERSION = {expectedVersion}
              AND (END_TIME IS NULL OR END_TIME = '0000-00-00 00:00:00')", cancellationToken);

        return updated == 1;
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
