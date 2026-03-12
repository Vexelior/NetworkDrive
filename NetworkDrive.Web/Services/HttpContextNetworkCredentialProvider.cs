using System.Security.Claims;
using NetworkDrive.Domain.Interfaces;

namespace NetworkDrive.Web.Services;

public class HttpContextNetworkCredentialProvider(IHttpContextAccessor httpContextAccessor) : INetworkCredentialProvider
{
    public const string PasswordClaimType = "NetworkPassword";

    public (string? Username, string? Password) GetCredentials()
    {
        var user = httpContextAccessor.HttpContext?.User;
        var username = user?.FindFirst(ClaimTypes.Name)?.Value;
        var password = user?.FindFirst(PasswordClaimType)?.Value;
        return (username, password);
    }
}
