namespace NetworkDrive.Domain.Entities;

public abstract class StorageItem
{
    public string? Name { get; protected set; }
    public string? RelativePath { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public DateTime ModifiedAt { get; protected set; }
}
