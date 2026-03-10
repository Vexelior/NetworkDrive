using MediatR;
using NetworkDrive.Domain.Interfaces;

namespace NetworkDrive.Application.UseCases.UploadFile;

public record UploadFileCommand(string FolderPath, string FileName, Stream Content) : IRequest<UploadFileResult>;

public record UploadFileResult(string SavedPath);

public class UploadFileHandler : IRequestHandler<UploadFileCommand, UploadFileResult>
{
    private readonly IStorageRepository _repo;

    public UploadFileHandler(IStorageRepository repo) => _repo = repo;

    public async Task<UploadFileResult> Handle(UploadFileCommand request, CancellationToken ct)
    {
        var safeName = Path.GetFileName(request.FileName); // strip path components
        var destination = Path.Combine(request.FolderPath, safeName);
        await _repo.SaveFileAsync(destination, request.Content, ct);
        return new UploadFileResult(destination);
    }
}
