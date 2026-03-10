namespace NetworkDrive.Domain.Entities;

public class StorageFolder : StorageItem
{
    public static StorageFolder Create(string name, string path, DateTime modifiedAt) => new() { Name = name, RelativePath = path, ModifiedAt = modifiedAt };
}
