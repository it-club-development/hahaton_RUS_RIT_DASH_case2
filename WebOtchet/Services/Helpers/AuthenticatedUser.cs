using WebOtchet.Models;

namespace WebOtchet.Services.Helpers;

public sealed record AuthenticatedUser(
    Guid Id,
    string Username,
    Roles Role
);
