using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Extensions.Options;
using Microsoft.Win32.SafeHandles;
using NetworkDrive.Domain.Interfaces;

namespace NetworkDrive.Infrastructure.Storage;

public class NetworkShareAuthService(IOptions<StorageOptions> options) : INetworkShareAuthService
{
    private const int Logon32LogonNewCredentials = 9;
    private const int Logon32ProviderWinnt50 = 3;

    public bool ValidateCredentials(string username, string password)
    {
        // Parse "DOMAIN\user" or use the share host as the domain.
        string domain;
        string user;

        if (username.Contains('\\'))
        {
            var parts = username.Split('\\', 2);
            domain = parts[0];
            user = parts[1];
        }
        else
        {
            domain = ExtractHost(options.Value.RootPath);
            user = username;
        }

        if (!LogonUser(user, domain, password,
                Logon32LogonNewCredentials, Logon32ProviderWinnt50,
                out var token))
        {
            return false;
        }

        // LOGON32_LOGON_NEW_CREDENTIALS always succeeds locally; the password
        // is only checked when the token is used against a remote resource.
        // Verify by actually probing the network share.
        using (token)
        {
            try
            {
                WindowsIdentity.RunImpersonated(token, () =>
                    Directory.Exists(options.Value.RootPath));
                // Attempt a lightweight directory listing to force authentication.
                WindowsIdentity.RunImpersonated(token, () =>
                    Directory.GetDirectories(options.Value.RootPath, "*", new EnumerationOptions { RecurseSubdirectories = false }));
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }
    }

    private static string ExtractHost(string uncPath)
    {
        // "\\server\share" → "server"
        var trimmed = uncPath.TrimStart('\\');
        var sep = trimmed.IndexOfAny(['\\', '/']);
        return sep >= 0 ? trimmed[..sep] : trimmed;
    }

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool LogonUser(
        string lpszUsername,
        string lpszDomain,
        string lpszPassword,
        int dwLogonType,
        int dwLogonProvider,
        out SafeAccessTokenHandle phToken);
}
