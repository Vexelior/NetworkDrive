using Microsoft.AspNetCore.Mvc;

namespace NetworkDrive.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return RedirectToAction("Index", "Drive");
    }
}
