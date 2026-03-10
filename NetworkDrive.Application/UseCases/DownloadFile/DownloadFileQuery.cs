using MediatR;
using NetworkDrive.Domain.Interfaces;

namespace NetworkDrive.Application.UseCases.DownloadFile;

public record DownloadFileQuery(string RelativePath) : IRequest<Stream>;

public class DownloadFileHandler : IRequestHandler<DownloadFileQuery, Stream>
{
    private readonly IStorageRepository _repo;

    public DownloadFileHandler(IStorageRepository repo) => _repo = repo;

    public async Task<Stream> Handle(DownloadFileQuery request, CancellationToken ct)
    {
        return await _repo.GetFileStreamAsync(request.RelativePath);
    }
}
