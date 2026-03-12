using Microsoft.Extensions.Options;
using NetworkDrive.Domain.Entities;
using NetworkDrive.Domain.Exceptions;
using NetworkDrive.Domain.Interfaces;

namespace NetworkDrive.Infrastructure.Storage;

public class LocalStorageRepository : IStorageRepository
{
    private readonly string _rootPath;
    private readonly INetworkImpersonator _impersonator;

    public LocalStorageRepository(IOptions<StorageOptions> options, INetworkImpersonator impersonator)
    {
        _rootPath = options.Value.RootPath;
        _impersonator = impersonator;
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
        return await _impersonator.RunAsync(() =>
        {
            var dir = new DirectoryInfo(fullPath);
            var items = new List<StorageItem>();

            foreach (var d in dir.GetDirectories())
                items.Add(StorageFolder.Create(d.Name, Path.GetRelativePath(_rootPath, d.FullName), d.LastWriteTime));

            foreach (var f in dir.GetFiles())
                items.Add(StorageFile.Create(f.Name, Path.GetRelativePath(_rootPath, f.FullName), f.Length, f.LastWriteTime));

            return Task.FromResult<IEnumerable<StorageItem>>(items);
        });
    }

    public async Task<Stream> GetFileStreamAsync(string relativePath) =>
        await _impersonator.RunAsync(() =>
            Task.FromResult<Stream>(File.OpenRead(Resolve(relativePath))));

    public async Task SaveFileAsync(string relativePath, Stream content, CancellationToken ct)
    {
        var fullPath = Resolve(relativePath);
        await _impersonator.RunAsync(async () =>
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            await using var fs = File.Create(fullPath);
            await content.CopyToAsync(fs, ct);
        });
    }

    public async Task CreateFolderAsync(string relativePath)
    {
        await _impersonator.RunAsync(() =>
        {
            Directory.CreateDirectory(Resolve(relativePath));
            return Task.CompletedTask;
        });
    }

    public async Task DeleteAsync(string relativePath)
    {
        var fullPath = Resolve(relativePath);
        await _impersonator.RunAsync(() =>
        {
            if (Directory.Exists(fullPath)) Directory.Delete(fullPath, recursive: true);
            else File.Delete(fullPath);
            return Task.CompletedTask;
        });
    }

    public async Task MoveAsync(string src, string dest)
    {
        await _impersonator.RunAsync(() =>
        {
            File.Move(Resolve(src), Resolve(dest));
            return Task.CompletedTask;
        });
    }

    public async Task<bool> ExistsAsync(string relativePath) =>
        await _impersonator.RunAsync(() =>
            Task.FromResult(Path.Exists(Resolve(relativePath))));
}
