using System.ComponentModel.DataAnnotations;

namespace WebOtchet.DTO.Users;

public sealed class ResetPasswordRequest
{
    [Required, StringLength(128, MinimumLength = 12)]
    public string NewPassword { get; init; } = "";
}

