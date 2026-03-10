using Microsoft.Extensions.Options;
using NetworkDrive.Domain.Entities;
using NetworkDrive.Domain.Exceptions;
using NetworkDrive.Domain.Interfaces;

namespace NetworkDrive.Infrastructure.Storage;

public class LocalStorageRepository : IStorageRepository
{
    private readonly string _rootPath;

    public LocalStorageRepository(IOptions<StorageOptions> options)
    {
        _rootPath = options.Value.RootPath;
    }

    private string Resolve(string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, relativePath));
        if (!fullPath.StartsWith(_rootPath, StringComparison.OrdinalIgnoreCase))
            throw new PathTraversalException(); // Security guard
        return fullPath;
    }

    public async Task<IEnumerable<StorageItem>> GetItemsAsync(string relativePath)
    {
        var fullPath = Resolve(relativePath);
        var dir = new DirectoryInfo(fullPath);
        var items = new List<StorageItem>();

        foreach (var d in dir.GetDirectories())
            items.Add(StorageFolder.Create(d.Name, Path.GetRelativePath(_rootPath, d.FullName), d.LastWriteTime));

        foreach (var f in dir.GetFiles())
            items.Add(StorageFile.Create(f.Name, Path.GetRelativePath(_rootPath, f.FullName), f.Length, f.LastWriteTime));

        return items;
    }

    public Task<Stream> GetFileStreamAsync(string relativePath) =>
        Task.FromResult<Stream>(File.OpenRead(Resolve(relativePath)));

    public async Task SaveFileAsync(string relativePath, Stream content, CancellationToken ct)
    {
        var fullPath = Resolve(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using var fs = File.Create(fullPath);
        await content.CopyToAsync(fs, ct);
    }

    public Task CreateFolderAsync(string relativePath)
    {
        Directory.CreateDirectory(Resolve(relativePath));
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string relativePath)
    {
        var fullPath = Resolve(relativePath);
        if (Directory.Exists(fullPath)) Directory.Delete(fullPath, recursive: true);
        else File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public Task MoveAsync(string src, string dest)
    {
        File.Move(Resolve(src), Resolve(dest));
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string relativePath) =>
        Task.FromResult(Path.Exists(Resolve(relativePath)));
}
