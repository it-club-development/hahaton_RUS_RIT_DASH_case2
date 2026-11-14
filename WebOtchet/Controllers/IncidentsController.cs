using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebOtchet.DTO.Incidents;
using WebOtchet.Services;

namespace WebOtchet.Controllers.Api;

[ApiController]
[Route("api/incidents")]
[Authorize]
public sealed class IncidentsController : ControllerBase
{
    private readonly IncidentService _service;

    public IncidentsController(IncidentService service)
    {
        _service = service;
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
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType<IncidentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] IncidentWriteRequest request,
        CancellationToken cancellationToken)
    {
        var incident = await _service.CreateAsync(
            request.ToEntity(),
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
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] IncidentWriteRequest request,
        CancellationToken cancellationToken)
    {
        var incident = request.ToEntity(id);

        var updated = await _service.UpdateAsync(
            incident,
            cancellationToken
        );

        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var deleted = await _service.DeleteAsync(id, cancellationToken);

        return deleted ? NoContent() : NotFound();
    }
}
