using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using WebOtchet.DTO.Users;
using WebOtchet.Services;

namespace WebOtchet.Controllers.Api;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public sealed class UsersController : ControllerBase
{
    private readonly UserService _service;

    public UsersController(UserService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        var users = await _service.GetAllAsync(cancellationToken);
        return Ok(users);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var user = await _service.GetByIdAsync(id, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _service.CreateAsync(
            request.Username,
            request.Password,
            request.Role,
            cancellationToken
        );

        return CreatedAtAction(
            nameof(GetById),
            new { id = user.Id },
            user
        );
    }

    [HttpPut("{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _service.UpdateDetailsAsync(
            id,
            request.Username,
            request.Role,
            cancellationToken
        );

        if (!updated)
            return NotFound();

        // Если администратор изменил собственную запись,
        // текущие claims/cookie уже могут быть устаревшими.
        if (TryGetCurrentUserId(out var currentUserId) && currentUserId == id)
            await HttpContext.SignOutAsync();

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (TryGetCurrentUserId(out var currentUserId) && currentUserId == id)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Нельзя удалить текущую учётную запись.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var deleted = await _service.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    private bool TryGetCurrentUserId(out Guid id)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out id);
    }
}
