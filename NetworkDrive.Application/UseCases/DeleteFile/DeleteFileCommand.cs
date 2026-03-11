using MediatR;
using NetworkDrive.Domain.Interfaces;

namespace NetworkDrive.Application.UseCases.DeleteFile;

public record DeleteFileCommand(string Path) : IRequest;

public class DeleteFileHandler : IRequestHandler<DeleteFileCommand>
{
    private readonly IStorageRepository _repo;

    public DeleteFileHandler(IStorageRepository repo) => _repo = repo;

    public async Task Handle(DeleteFileCommand request, CancellationToken ct)
    {
        await _repo.DeleteAsync(request.Path);
    }
}
