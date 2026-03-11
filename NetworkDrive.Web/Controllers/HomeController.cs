using Microsoft.AspNetCore.Mvc;
using NetworkDrive.Domain.Interfaces;

namespace NetworkDrive.Web.Controllers;

public class HomeController(INetworkShareAuthService shareAuth) : Controller
{
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Drive");

        return RedirectToAction("Login", "Auth");
    }
}
