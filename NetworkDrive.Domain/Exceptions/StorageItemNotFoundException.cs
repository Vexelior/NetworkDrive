namespace NetworkDrive.Domain.Exceptions;

public class StorageItemNotFoundException(string path) : DomainException($"Item not found: '{path}'")
{
}
