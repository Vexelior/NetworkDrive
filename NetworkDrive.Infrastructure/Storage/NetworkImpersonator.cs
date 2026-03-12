using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;
using Microsoft.Extensions.Options;
using NetworkDrive.Domain.Interfaces;

namespace NetworkDrive.Infrastructure.Storage;

public class NetworkImpersonator(
    INetworkCredentialProvider credentialProvider,
    IOptions<StorageOptions> options) : INetworkImpersonator
{
    private const int LOGON32_LOGON_NEW_CREDENTIALS = 9;
    private const int LOGON32_PROVIDER_WINNT50 = 3;

    public async Task<T> RunAsync<T>(Func<Task<T>> action)
    {
        var (username, password) = credentialProvider.GetCredentials();
        if (username is null || password is null)
            return await action();

        ParseDomainUser(username, out var domain, out var user);

        if (!LogonUser(user, domain, password,
                LOGON32_LOGON_NEW_CREDENTIALS, LOGON32_PROVIDER_WINNT50,
                out var token))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        using (token)
        {
            return await WindowsIdentity.RunImpersonatedAsync(token, action);
        }
    }

    public async Task RunAsync(Func<Task> action)
    {
        await RunAsync(async () => { await action(); return 0; });
    }

    private void ParseDomainUser(string username, out string domain, out string user)
    {
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
    }

    private static string ExtractHost(string uncPath)
    {
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
