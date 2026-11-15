using System.Globalization;
using System.ComponentModel.DataAnnotations;
using WebOtchet.Models;
using WebOtchet.DTO.Incidents;
using WebOtchet.Repositories.IRepository;
using WebOtchet.Services.Helpers;

namespace WebOtchet.Services;

public sealed class IncidentService
{
    private readonly IIncidentRepository _repository;

    private static readonly TimeSpan MaxDatabaseTime =
        new(838, 59, 59);

    private const int MaxPageSize = 200;

    private static readonly HashSet<string> Directions = new(
        ["ХМЯН", "СВН", "ЮЗН", "Вся сеть"],
        StringComparer.OrdinalIgnoreCase
    );

    private static readonly HashSet<string> TechCenters = new(
        [
            "НБР", "АСБ", "ИРБ", "КРТ", "НТГ", "НТР",
            "НУРЛ", "НВРТ", "НУРГ", "ОКТ", "СЛХ", "СОВ",
            "СРГ", "ХМН", "ЕКБ", "КМУ", "КРГ", "НСР"
        ],
        StringComparer.OrdinalIgnoreCase
    );

    private static readonly HashSet<string> NetworkTypes = new(
        ["GSM", "LTE", "LTE/GSM"],
        StringComparer.OrdinalIgnoreCase
    );

    private static readonly HashSet<string> AlarmTypes = new(
        [
            "РРС", "ССС", "ЭЛ", "RBS", "МСПД",
            "ТМ", "ОПС", "ПР", "АВР", "Аренда"
        ],
        StringComparer.OrdinalIgnoreCase
    );

    public IncidentService(IIncidentRepository repository)
    {
        _repository = repository;
    }

    public async Task<Incident?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ValidationException("Некорректный ID аварии.");

        var incident = await _repository.GetByIdAsync(
            id,
            cancellationToken
        );

        if (incident is not null)
            RefreshLength(incident);

        return incident;
    }

    public async Task<PageResult<Incident>> GetReportAsync(
        DateTime from,
        DateTime to,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default,
        IncidentReportFilter? filter = null)
    {
        ValidatePeriod(from, to);

        if (page < 1)
            throw new ValidationException(
                "Номер страницы должен быть больше нуля."
            );

        if (pageSize < 1 || pageSize > MaxPageSize)
            throw new ValidationException(
                $"Размер страницы должен быть от 1 до {MaxPageSize}."
            );

        int skip;

        try
        {
            skip = checked((page - 1) * pageSize);
        }
        catch (OverflowException)
        {
            throw new ValidationException(
                "Слишком большой номер страницы."
            );
        }

        var totalCount = await _repository.CountByPeriodAsync(
            from,
            to,
            filter,
            cancellationToken
        );

        var incidents = await _repository.GetByPeriodAsync(
            from,
            to,
            skip,
            pageSize,
            filter,
            cancellationToken
        );

        foreach (var incident in incidents)
            RefreshLength(incident);

        return new PageResult<Incident>(
            incidents,
            totalCount,
            page,
            pageSize
        );
    }


    public async IAsyncEnumerable<Incident> StreamReportAsync(DateTime from, DateTime to,
        IncidentReportFilter filter, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ValidatePeriod(from, to);
        var now = DateTime.Now;
        await foreach (var incident in _repository.StreamByPeriod(from, to, filter).WithCancellation(cancellationToken))
        {
            RefreshLength(incident, now);
            yield return incident;
        }
    }

    public async Task<IncidentReportSummary> GetSummaryAsync(DateTime from, DateTime to,
        IncidentReportFilter filter, CancellationToken cancellationToken, int? hours = null)
    {
        ValidatePeriod(from, to);
        if (hours is { } window)
        {
            var now = DateTime.Now;
            var start = now.AddHours(-window);
            from = from > start ? from : start;
            to = to < now ? to : now;
            if (from >= to)
                return new IncidentReportSummary(0, 0, 0, new Dictionary<string, long>());
        }
        int open = 0, closed = 0, skipped = 0;
        var downtime = new Dictionary<string, long>();
        await foreach (var row in _repository.GetReportValues(from, to, filter).WithCancellation(cancellationToken))
        {
            if (row.EndTime is null || row.EndTime == DateTime.MinValue)
                open++;
            else
                closed++;

            if (row.Downtime is not { } time)
            {
                skipped++;
                continue;
            }
            var direction = string.IsNullOrEmpty(row.Direction) ? "Без направления" : row.Direction;
            downtime[direction] = downtime.GetValueOrDefault(direction) + time.Ticks / TimeSpan.TicksPerSecond;
        }
        return new IncidentReportSummary(open, closed, skipped, downtime);
    }

    public async Task<Incident> CreateAsync(
        Incident incident,
        AuthenticatedUser currentUser,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(incident);

        if (!CanCreateIncident(currentUser))
        throw new UnauthorizedAccessException(
            "Недостаточно прав для создания аварии.");
        ValidateAndNormalize(incident);

            incident.Id = Guid.NewGuid();
            incident.CreatedByUserId = currentUser.Id;

        CalculateDerivedFields(incident);

        await _repository.AddAsync(
            incident,
            cancellationToken
        );

        return incident;
    }


    public async Task<bool> UpdateAsync(
        Incident incident,
        AuthenticatedUser currentUser,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(incident);

        if (incident.Id == Guid.Empty)
            throw new ValidationException(
                "Не указан идентификатор аварии."
            );
        var existing = await _repository.GetByIdAsync(incident.Id, cancellationToken);
        if (existing is null)
            return false;

        if (!CanManageIncident(currentUser, existing))
            throw new UnauthorizedAccessException("Недостаточно прав для изменения записи.");

        ValidateAndNormalize(incident);

        CalculateDerivedFields(incident);

        return await _repository.UpdateAsync(
            incident,
            cancellationToken
        );
    }


    public async Task<int> CloseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var incident = await _repository.GetByIdAsync(id, cancellationToken);
        if (incident is null)
            return 404;
        if (incident.EndTime.HasValue)
            return 204;

        var now = DateTime.Now;
        if (now < incident.BeginTime)
            throw new ValidationException("Нельзя закрыть аварию раньше её начала.");

        incident.EndTime = new DateTime(now.Ticks - now.Ticks % TimeSpan.TicksPerSecond);
        RefreshLength(incident);
        if (await _repository.CloseAsync(incident, cancellationToken))
            return 204;

        var current = await _repository.GetByIdAsync(id, cancellationToken);
        return current is null ? 404 : current.EndTime.HasValue ? 204 : 409;
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        AuthenticatedUser currentUser,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ValidationException(
                "Некорректный ID аварии."
            );
        var incident = await _repository.GetByIdAsync(id, cancellationToken);
        if (incident is null)
            return false;

        if (!CanManageIncident(currentUser, incident))
            throw new UnauthorizedAccessException("Недостаточно прав для удаления записи.");
        return await _repository.DeleteAsync(
            id,
            cancellationToken
        );
    }


    private static void ValidateAndNormalize(Incident incident)
    {
        var errors = new List<string>();

        incident.Pw = NormalizeText(
            incident.Pw, "Номер PW/NTT", 450, true, errors);

        incident.Direction = NormalizeChoice(
            incident.Direction, "Направление", Directions, errors);

        incident.TechCenter = NormalizeChoice(
            incident.TechCenter, "ТЦ", TechCenters, errors);

        incident.NetworkType = NormalizeChoice(
            incident.NetworkType, "Тип сети", NetworkTypes, errors);

        incident.AlarmType = NormalizeChoice(
            incident.AlarmType, "Тип аварии", AlarmTypes, errors);

        incident.ServiceElement = NormalizeText(
            incident.ServiceElement, "Сетевой элемент", 450, false, errors);

        incident.EquipmentType = NormalizeText(
            incident.EquipmentType, "Тип оборудования", 450, false, errors);

        incident.Description = NormalizeText(
            incident.Description, "Описание", 450, false, errors);

        incident.AlarmWho = NormalizeText(
            incident.AlarmWho, "Уведомление об аварии", 450, false, errors);

        incident.NoticeDescription = NormalizeText(
            incident.NoticeDescription, "Подробное описание", 450, false, errors);

        if (incident.Category is < 1 or > 3)
            errors.Add("Категория должна быть 1, 2 или 3.");
        incident.NumberOfBaseStations =
            (incident.NumberOfBaseStations ?? "")
            .Trim()
            .ToUpperInvariant();

        ValidateBaseStationCount(
            incident.NumberOfBaseStations,
            errors
        );

        if (incident.BeginTime.Year < 1000)
            errors.Add("Год начала аварии должен быть не ранее 1000.");

        if (incident.EndTime is { Year: < 1000 })
            errors.Add("Год окончания аварии должен быть не ранее 1000.");

        if (incident.EndTime.HasValue &&
            incident.EndTime.Value < incident.BeginTime)
        {
            errors.Add(
                "Дата окончания не может быть раньше даты начала."
            );
        }

        ValidateTime(
            incident.Akb, "АКБ", errors);

        ValidateTime(
            incident.GeneratorTime, "ГУ", errors);

        ValidateTime(
            incident.BaseStationTime, "Время прерыва связи", errors);

        if (errors.Count > 0)
        {
            throw new ValidationException(
                string.Join(Environment.NewLine, errors)
            );
        }
    }

    private static string NormalizeText(
        string? value,
        string field,
        int maxLength,
        bool required,
        List<string> errors)
    {
        var result = (value ?? "").Trim();

        if (required && string.IsNullOrWhiteSpace(result))
            errors.Add($"Поле «{field}» обязательно.");

        if (result.Length > maxLength)
        {
            errors.Add(
                $"Поле «{field}» не должно превышать {maxLength} символов."
            );
        }

        return result;
    }

    private static string NormalizeChoice(
        string? value,
        string field,
        HashSet<string> allowedValues,
        List<string> errors)
    {
        var normalized = (value ?? "").Trim();

        var actualValue = allowedValues.FirstOrDefault(
            x => string.Equals(
                x,
                normalized,
                StringComparison.OrdinalIgnoreCase
            )
        );

        if (actualValue is null)
        {
            errors.Add($"Недопустимое значение поля «{field}».");
            return normalized;
        }

        return actualValue;
    }

    private static void ValidateBaseStationCount(
        string value,
        List<string> errors)
    {
        if (value == "С")
            return;

        if (!int.TryParse(
                value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var count)
            || count < 1
            || count > 10000)
        {
            errors.Add(
                "Количество БС должно быть числом от 1 до 10000 либо «С»."
            );
        }
    }

    private static void ValidateTime(
        TimeSpan? value,
        string field,
        List<string> errors)
    {
        if (!value.HasValue)
            return;

        if (value.Value < TimeSpan.Zero ||
            value.Value > MaxDatabaseTime)
        {
            errors.Add(
                $"Поле «{field}» должно находиться в диапазоне 00:00:00–838:59:59."
            );

            return;
        }

        if (value.Value.Ticks % TimeSpan.TicksPerSecond != 0)
        {
            errors.Add(
                $"Поле «{field}» должно иметь точность до целых секунд."
            );
        }
    }

    private static void ValidatePeriod(DateTime from, DateTime to)
    {
        if (from == default || to == default || to <= from)
        {
            throw new ValidationException(
                "Указан некорректный период отчёта."
            );
        }
    }


    private static void CalculateDerivedFields(Incident incident)
    {

        RefreshLength(incident);


        if (!incident.BaseStationTime.HasValue ||
            incident.NumberOfBaseStations == "С")
        {
            incident.CommonStationTime = null;
            return;
        }

        int count = int.Parse(
            incident.NumberOfBaseStations,
            CultureInfo.InvariantCulture
        );

        long ticks = checked(
            incident.BaseStationTime.Value.Ticks * (long)count
        );

        var totalTime = TimeSpan.FromTicks(ticks);

        incident.CommonStationTime = totalTime;
    }

    private static void RefreshLength(Incident incident, DateTime? now = null)
    {
        var duration = (incident.EndTime ?? now ?? DateTime.Now) - incident.BeginTime;

        if (duration < TimeSpan.Zero)
            duration = TimeSpan.Zero;

        long totalHours =
            duration.Ticks / TimeSpan.TicksPerHour;

        incident.Length = string.Format(
            CultureInfo.InvariantCulture,
            "{0:00}:{1:00}:{2:00}",
            totalHours,
            duration.Minutes,
            duration.Seconds
        );
    }
private static bool CanCreateIncident(AuthenticatedUser user)
{
    return user.Role is Roles.Admin or Roles.Attendant;
}

private static bool CanManageIncident(
    AuthenticatedUser user,
    Incident incident)
{
    return user.Role switch
    {
        Roles.Admin => true,

        Roles.Attendant =>
            incident.CreatedByUserId == user.Id,

        Roles.Engineer => false,

        _ => false
    };
}}
