using MediatR;
using Microsoft.AspNetCore.Mvc;
using NetworkDrive.Application.UseCases.BrowseFolder;
using NetworkDrive.Application.UseCases.UploadFile;
using NetworkDrive.Application.UseCases.DownloadFile;

namespace NetworkDrive.Web.Controllers
{
    public class DriveController(IMediator mediator) : Controller
    {
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
    }
}
