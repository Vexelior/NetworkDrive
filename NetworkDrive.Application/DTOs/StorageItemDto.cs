using NetworkDrive.Domain.Entities;

namespace NetworkDrive.Application.DTOs;

public record StorageItemDto(string? Name,
                             string? RelativePath,
                             bool IsFolder,
                             long? SizeInBytes,
                             DateTime ModifiedAt)
{
    public static StorageItemDto FromEntity(StorageItem item) => item switch
    {
        StorageFile f => new(f.Name, f.RelativePath, false, f.SizeInBytes, f.ModifiedAt),
        StorageFolder d => new(d.Name, d.RelativePath, true, null, d.ModifiedAt),
        _ => throw new ArgumentOutOfRangeException()
    };
}
