namespace NetworkDrive.Domain.Interfaces;

public interface INetworkImpersonator
{
    Task<T> RunAsync<T>(Func<Task<T>> action);
    Task RunAsync(Func<Task> action);
}
