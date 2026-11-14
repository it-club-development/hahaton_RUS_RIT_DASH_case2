using System.Security.Cryptography;
using System.Text;

namespace WebOtchet.Services.Helpers;

public static class SessionStamp
{
    public const string ClaimType = "motiv:password-stamp";

    public static string FromHash(string hash) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(hash)));
}
