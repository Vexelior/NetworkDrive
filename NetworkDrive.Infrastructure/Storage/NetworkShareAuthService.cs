using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;
using NetworkDrive.Domain.Interfaces;

namespace NetworkDrive.Infrastructure.Storage;

public class NetworkShareAuthService(IOptions<StorageOptions> options) : INetworkShareAuthService
{
    private const int NO_ERROR = 0;
    private const int ERROR_SESSION_CREDENTIAL_CONFLICT = 1219;
    private const int RESOURCETYPE_DISK = 1;

    public bool ValidateCredentials(string username, string password)
    {
        var remotePath = options.Value.RootPath;

        var netResource = new NETRESOURCE
        {
            dwType = RESOURCETYPE_DISK,
            lpRemoteName = remotePath
        };

        // First, try to cancel any existing connection so we can test fresh credentials.
        WNetCancelConnection2(remotePath, 0, true);

        var result = WNetAddConnection2(ref netResource, password, username, 0);

        if (result == NO_ERROR)
        {
            WNetCancelConnection2(remotePath, 0, true);
            return true;
        }

        if (result == ERROR_SESSION_CREDENTIAL_CONFLICT)
        {
            WNetCancelConnection2(remotePath, 0, true);
            result = WNetAddConnection2(ref netResource, password, username, 0);

            if (result == NO_ERROR)
            {
                WNetCancelConnection2(remotePath, 0, true);
                return true;
            }
        }

        return false;
    }

    [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
    private static extern int WNetAddConnection2(ref NETRESOURCE netResource, string password, string username, int flags);

    [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
    private static extern int WNetCancelConnection2(string name, int flags, bool force);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NETRESOURCE
    {
        public int dwScope;
        public int dwType;
        public int dwDisplayType;
        public int dwUsage;
        public string? lpLocalName;
        public string? lpRemoteName;
        public string? lpComment;
        public string? lpProvider;
    }
}
