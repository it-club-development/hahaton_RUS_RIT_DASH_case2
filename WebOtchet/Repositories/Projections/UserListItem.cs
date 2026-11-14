using WebOtchet.Models;

namespace WebOtchet.Repositories.Projections;

public sealed record UserListItem(
    Guid Id,
    string Username,
    Roles Role
);
