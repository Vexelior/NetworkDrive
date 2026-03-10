using NetworkDrive.Domain.Entities;

namespace NetworkDrive.Domain.Interfaces;

public interface IStorageRepository
{
    Task<IEnumerable<StorageItem>> GetItemsAsync(string relativePath);
    Task<Stream> GetFileStreamAsync(string relativePath);
    Task SaveFileAsync(string relativePath, Stream content, CancellationToken ct);
    Task CreateFolderAsync(string relativePath);
    Task DeleteAsync(string relativePath);
    Task MoveAsync(string sourcePath, string destinationPath);
    Task<bool> ExistsAsync(string relativePath);
}
