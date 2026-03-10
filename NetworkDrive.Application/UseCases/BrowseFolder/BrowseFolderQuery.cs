using MediatR;
using NetworkDrive.Application.DTOs;

namespace NetworkDrive.Application.UseCases.BrowseFolder;

public record BrowseFolderQuery(string RelativePath) : IRequest<BrowseFolderResult>;
public record BrowseFolderResult(IEnumerable<StorageItemDto> Items, string CurrentPath);
