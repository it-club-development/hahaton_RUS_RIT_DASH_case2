using System.ComponentModel.DataAnnotations;
using WebOtchet.Models;

namespace WebOtchet.DTO.Users;

public sealed class CreateUserRequest
{
    [Required, StringLength(100, MinimumLength = 3)]
    public string Username { get; init; } = "";

    [Required, StringLength(128, MinimumLength = 12)]
    public string Password { get; init; } = "";

    public Roles Role { get; init; } = Roles.Engineer;
}
