namespace NetworkDrive.Domain.Interfaces;

public interface INetworkCredentialProvider
{
    (string? Username, string? Password) GetCredentials();
}
