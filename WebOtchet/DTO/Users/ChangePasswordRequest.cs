using System.ComponentModel.DataAnnotations;

namespace WebOtchet.DTO.Users;

public sealed class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; init; } = "";

    [Required, StringLength(128, MinimumLength = 12)]
    public string NewPassword { get; init; } = "";
}
