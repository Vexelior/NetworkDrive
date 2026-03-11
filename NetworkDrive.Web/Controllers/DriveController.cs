using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using NetworkDrive.Application.UseCases.BrowseFolder;
using NetworkDrive.Application.UseCases.UploadFile;
using NetworkDrive.Application.UseCases.DownloadFile;
using NetworkDrive.Application.UseCases.DeleteFile;
using NetworkDrive.Domain.Interfaces;

namespace NetworkDrive.Web.Controllers
{
    [Authorize]
    public class DriveController(IMediator mediator, ITranscodingService transcodingService) : Controller
    {
        private readonly FileExtensionContentTypeProvider _contentTypeProvider = new(new Dictionary<string, string>(
            new FileExtensionContentTypeProvider().Mappings, StringComparer.OrdinalIgnoreCase)
        {
            [".mkv"] = "video/x-matroska",
            [".flac"] = "audio/flac",
            [".m4a"] = "audio/mp4",
            [".mp4"] = "video/mp4",
        });

        public async Task<IActionResult> Index(string path = "")
        {
            var result = await mediator.Send(new BrowseFolderQuery(path));
            return View(result);
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file, string path = "")
        {
            await mediator.Send(new UploadFileCommand(path, file.FileName, file.OpenReadStream()));
            return RedirectToAction("Index", new { path });
        }

        public async Task<IActionResult> Download(string path)
        {
            var stream = await mediator.Send(new DownloadFileQuery(path));
            return File(stream, "application/octet-stream", Path.GetFileName(path));
        }

        public async Task<IActionResult> Preview(string path, CancellationToken ct)
        {
            var fileName = Path.GetFileName(path);

            if (transcodingService.RequiresTranscoding(fileName))
            {
                var transcodedStream = await transcodingService.GetTranscodedStreamAsync(path, ct);
                return File(transcodedStream, "video/mp4");
            }

            var stream = await mediator.Send(new DownloadFileQuery(path), ct);

            if (!_contentTypeProvider.TryGetContentType(fileName, out var contentType))
                contentType = "application/octet-stream";

            return File(stream, contentType);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string path)
        {
            var parentPath = Path.GetDirectoryName(path) ?? "";
            await mediator.Send(new DeleteFileCommand(path));
            return RedirectToAction("Index", new { path = parentPath });
        }
    }
}
