using System.ComponentModel.DataAnnotations;

namespace WebOtchet.DTO.Users;

public sealed class LoginRequest
{
    [Required, StringLength(100, MinimumLength = 3)]
    public string Username { get; init; } = "";

    [Required]
    public string Password { get; init; } = "";

    public bool RememberMe { get; init; }
}
