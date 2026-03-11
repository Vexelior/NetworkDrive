namespace NetworkDrive.Domain.Interfaces;

public interface INetworkShareAuthService
{
    bool ValidateCredentials(string username, string password);
}
