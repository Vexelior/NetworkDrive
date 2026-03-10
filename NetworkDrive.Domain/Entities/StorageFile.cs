using NetworkDrive.Domain.Exceptions;

namespace NetworkDrive.Domain.Entities;

public class StorageFile : StorageItem
{
    public long SizeInBytes { get; private set; }
    public string? Extension { get; private set; }

    public static StorageFile Create(string name, string path, long size, DateTime modifiedAt)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("File name cannot be empty.");
        return new StorageFile { Name = name, RelativePath = path, SizeInBytes = size, Extension = Path.GetExtension(name), ModifiedAt = modifiedAt };
    }
}

