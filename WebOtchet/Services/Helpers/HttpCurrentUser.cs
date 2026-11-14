using System.Security.Claims;
using WebOtchet.Models;

namespace WebOtchet.Services.Helpers;

public sealed class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public AuthenticatedUser? User
    {
        get
        {
            var principal = _httpContextAccessor.HttpContext?.User;

            if (principal?.Identity?.IsAuthenticated != true)
                return null;

            var idClaim = principal.FindFirstValue(
                ClaimTypes.NameIdentifier);

            var username = principal.FindFirstValue(
                ClaimTypes.Name);

            var roleClaim = principal.FindFirstValue(
                ClaimTypes.Role);

            if (!Guid.TryParse(idClaim, out var id))
                return null;

            if (string.IsNullOrWhiteSpace(username))
                return null;

            if (!Enum.TryParse<Roles>(
                    roleClaim, true, out var role))
                return null;

            if (!Enum.IsDefined(typeof(Roles), role))
                return null;

            return new AuthenticatedUser(id, username, role);
        }
    }
}
