namespace WebOtchet.Services.Helpers;

public interface ICurrentUser
{
    AuthenticatedUser? User { get; }
}
