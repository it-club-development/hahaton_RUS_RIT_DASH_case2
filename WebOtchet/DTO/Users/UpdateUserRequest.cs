using System.ComponentModel.DataAnnotations;
using WebOtchet.Models;

namespace WebOtchet.DTO.Users;

public sealed class UpdateUserRequest
{
    [Required, StringLength(100, MinimumLength = 3)]
    public string Username { get; init; } = "";

    public required Roles Role { get; init; }
}
