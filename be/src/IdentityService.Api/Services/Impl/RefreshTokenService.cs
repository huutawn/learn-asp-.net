using System.Security.Cryptography;
using System.Text;

namespace IdentityService.Api.Services;

public sealed class RefreshTokenService
{
    public string Generate()
    {
        var bytes =
            RandomNumberGenerator.GetBytes(64);

        return Convert.ToHexString(bytes);
    }

    public string Hash(string token)
    {
        var bytes =
            Encoding.UTF8.GetBytes(token);

        var hash =
            SHA256.HashData(bytes);

        return Convert.ToHexString(hash);
    }
}
