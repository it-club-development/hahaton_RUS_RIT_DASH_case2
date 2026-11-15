using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebOtchet.Models;
using WebOtchet.DTO;
using WebOtchet.DTO.Incidents;
using WebOtchet.Services;
using WebOtchet.Services.Helpers;

namespace WebOtchet.Controllers.Api;

[ApiController]
[Route("api/reports")]
[Authorize]
public sealed class ReportsController : ControllerBase
{
    private readonly IncidentService _service;

    public ReportsController(IncidentService service)
    {
        _service = service;
    }

    [HttpGet("incidents")]
    [ProducesResponseType<PagedResponse<IncidentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetIncidents(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] IncidentReportFilter filter,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.GetReportAsync(
            from,
            to,
            page,
            pageSize,
            cancellationToken,
            filter
        );

        var response = new PagedResponse<IncidentResponse>(
            result.Items.Select(IncidentResponse.FromEntity).ToArray(),
            result.TotalCount,
            result.Page,
            result.PageSize,
            result.TotalPages
        );

        return Ok(response);
    }

    [HttpGet("incidents/summary")]
    public async Task<ActionResult<IncidentReportSummary>> GetSummary(
        [FromQuery] DateTime from, [FromQuery] DateTime to,
        [FromQuery] IncidentReportFilter filter, [FromQuery, System.ComponentModel.DataAnnotations.Range(1, 720)] int? hours,
        CancellationToken cancellationToken)
        => Ok(await _service.GetSummaryAsync(from, to, filter, cancellationToken, hours));

    [HttpGet("incidents.csv")]
    [Produces("text/csv")]
    public async Task<IActionResult> ExportIncidentsCsv(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] IncidentReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream();
        await using var writer = new StreamWriter(
            stream,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: true),
            bufferSize: 1024,
            leaveOpen: true
        );

        string[] headers =
        [
            "ID", "PW", "Направление", "ТЦ", "Категория", "Тип сети",
            "Сетевой элемент", "Количество БС", "Тип оборудования", "Тип",
            "Описание", "Начало", "Окончание", "АКБ", "ГУ",
            "Длительность", "Время прерыва связи", "Общее время простоя БС",
            "Уведомление об аварии", "Подробное описание аварии"
        ];

        await writer.WriteLineAsync(string.Join(";", headers.Select(CsvEscape)));

        await foreach (var incident in _service.StreamReportAsync(from, to, filter, cancellationToken))
            await writer.WriteLineAsync(ToCsvRow(incident));

        await writer.FlushAsync(cancellationToken);
        stream.Position = 0;

        var fileName = $"incidents_{from:yyyyMMdd}_{to:yyyyMMdd}.csv";

        return File(
            stream.ToArray(),
            "text/csv; charset=utf-8",
            fileName
        );
    }

    private static string ToCsvRow(Incident x)
    {
        string[] values =
        [
            x.Id.ToString(),
            x.Pw,
            x.Direction,
            x.TechCenter,
            x.Category.ToString(CultureInfo.InvariantCulture),
            x.NetworkType,
            x.ServiceElement,
            x.NumberOfBaseStations,
            x.EquipmentType,
            x.AlarmType,
            x.Description,
            FormatDateTime(x.BeginTime),
            x.EndTime is { } end && end != DateTime.MinValue ? FormatDateTime(end) : "НВ",
            FormatTime(x.Akb),
            FormatTime(x.GeneratorTime),
            x.Length,
            FormatTime(x.BaseStationTime),
            FormatTime(x.CommonStationTime),
            x.AlarmWho,
            x.NoticeDescription
        ];

        return string.Join(";", values.Select(CsvEscape));
    }

    private static string FormatDateTime(DateTime value) =>
        value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

    private static string FormatTime(TimeSpan? value)
    {
        if (value is null)
            return "";

        var time = value.Value;
        var totalHours = (long)time.TotalHours;

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{totalHours:00}:{time.Minutes:00}:{time.Seconds:00}"
        );
    }

    private static string CsvEscape(string? value)
    {
        value ??= "";

        var trimmed = value.TrimStart();
        if (trimmed.StartsWith('=') || trimmed.StartsWith('+') ||
            trimmed.StartsWith('-') || trimmed.StartsWith('@'))
        {
            value = "'" + value;
        }

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

}

