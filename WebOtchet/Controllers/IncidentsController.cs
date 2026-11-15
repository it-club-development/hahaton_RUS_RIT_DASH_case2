using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebOtchet.DTO.Incidents;
using WebOtchet.Services;
using WebOtchet.Services.Helpers;
namespace WebOtchet.Controllers.Api;

[ApiController]
[Route("api/incidents")]
[Authorize]
public sealed class IncidentsController : ControllerBase
{
    private readonly IncidentService _service;
    private readonly ICurrentUser _currentUser;

    public IncidentsController(IncidentService service, ICurrentUser currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<IncidentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var incident = await _service.GetByIdAsync(id, cancellationToken);

        if (incident is null)
            return NotFound();

        return Ok(IncidentResponse.FromEntity(incident));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Attendant")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType<IncidentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] IncidentWriteRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = _currentUser.User;

        if (currentUser is null)
            return Unauthorized();

        var incident = await _service.CreateAsync(
            request.ToEntity(),
            currentUser,
            cancellationToken
        );

        var response = IncidentResponse.FromEntity(incident);

        return CreatedAtAction(
            nameof(GetById),
            new { id = incident.Id },
            response
        );
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Attendant")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] IncidentWriteRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Version is null)
            return BadRequest(new ProblemDetails { Title = "Для изменения записи укажите её версию." });

        var incident = request.ToEntity(id);
var currentUser = _currentUser.User;

if (currentUser is null)
    return Unauthorized();

        var updated = await _service.UpdateAsync(
            incident,
            currentUser,
            cancellationToken
        );

        return updated ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/close")]
    [Authorize(Roles = "Admin,Attendant")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(Guid id, CancellationToken cancellationToken)
    {
        var status = await _service.CloseAsync(id, cancellationToken);
        return status == StatusCodes.Status409Conflict
            ? Conflict(new ProblemDetails { Title = "Запись изменилась. Обновите отчёт и повторите попытку." })
            : StatusCode(status);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Attendant")] 
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var currentUser = _currentUser.User;

        if (currentUser is null)
            return Unauthorized();

        var deleted = await _service.DeleteAsync(id, currentUser, cancellationToken);

        return deleted ? NoContent() : NotFound();
    }
}
