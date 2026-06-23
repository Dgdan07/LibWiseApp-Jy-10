using Microsoft.AspNetCore.Mvc;

namespace LibWiseApp.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return RedirectToAction("Login", "Account");
    }
}
