namespace NetworkDrive.Domain.Exceptions
{
    public class PathTraversalException() : DomainException("Access outside the root storage path is not allowed.");
}
