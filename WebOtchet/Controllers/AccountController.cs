using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebOtchet.DTO.Users;
using WebOtchet.Services;

namespace WebOtchet.Controllers.Api;

[ApiController]
[Route("api/account")]
public sealed class AccountController : ControllerBase
{
    private readonly UserService _service;
    private readonly IAntiforgery _antiforgery;

    public AccountController(
        UserService service,
        IAntiforgery antiforgery)
    {
        _service = service;
        _antiforgery = antiforgery;
    }

    [HttpGet("antiforgery")]
    [AllowAnonymous]
    public IActionResult GetAntiforgeryToken()
    {
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { requestToken = tokens.RequestToken });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _service.AuthenticateAsync(
            request.Username,
            request.Password,
            cancellationToken
        );

        if (user is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Неверный логин или пароль.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme
        );

        var principal = new ClaimsPrincipal(identity);

        var properties = new AuthenticationProperties
        {
            IsPersistent = request.RememberMe,
            AllowRefresh = true,
            ExpiresUtc = DateTimeOffset.UtcNow.Add(
                request.RememberMe
                    ? TimeSpan.FromDays(7)
                    : TimeSpan.FromHours(8)
            )
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            properties
        );

        return Ok(new
        {
            user.Id,
            user.Username,
            Role = user.Role.ToString()
        });
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(idValue, out var id))
            return Unauthorized();

        return Ok(new
        {
            Id = id,
            Username = User.Identity?.Name,
            Role = User.FindFirstValue(ClaimTypes.Role)
        });
    }

    [HttpPost("logout")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme
        );

        return NoContent();
    }

    [HttpPost("change-password")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(idValue, out var id))
            return Unauthorized();

        var changed = await _service.ChangePasswordAsync(
            id,
            request.CurrentPassword,
            request.NewPassword,
            cancellationToken
        );

        if (!changed)
            return NotFound();

        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme
        );

        return NoContent();
    }
}
