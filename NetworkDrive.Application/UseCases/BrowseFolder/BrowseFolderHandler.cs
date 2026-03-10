using MediatR;
using NetworkDrive.Application.DTOs;
using NetworkDrive.Domain.Interfaces;

namespace NetworkDrive.Application.UseCases.BrowseFolder;

public class BrowseFolderHandler : IRequestHandler<BrowseFolderQuery, BrowseFolderResult>
{
    private readonly IStorageRepository _repo;

    public BrowseFolderHandler(IStorageRepository repo) => _repo = repo;

    public async Task<BrowseFolderResult> Handle(BrowseFolderQuery request, CancellationToken ct)
    {
        var items = await _repo.GetItemsAsync(request.RelativePath);
        var dtos = items.Select(StorageItemDto.FromEntity);
        return new BrowseFolderResult(dtos, request.RelativePath);
    }
}
