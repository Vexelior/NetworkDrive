using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;
using NetworkDrive.Domain.Interfaces;

namespace NetworkDrive.Infrastructure.Storage;

public class NetworkShareAuthService(IOptions<StorageOptions> options) : INetworkShareAuthService
{
    private const int LOGON32_LOGON_NEW_CREDENTIALS = 9;
    private const int LOGON32_PROVIDER_WINNT50 = 3;

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
                LOGON32_LOGON_NEW_CREDENTIALS, LOGON32_PROVIDER_WINNT50,
                out var token))
        {
            return false;
        }

        CloseHandle(token);
        return true;
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
        out nint phToken);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(nint hObject);
}
