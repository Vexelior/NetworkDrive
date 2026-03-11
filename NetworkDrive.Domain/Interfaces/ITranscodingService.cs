namespace NetworkDrive.Domain.Interfaces;

public interface ITranscodingService
{
    bool RequiresTranscoding(string fileName);
    Task<Stream> GetTranscodedStreamAsync(string relativePath, CancellationToken ct = default);
}
