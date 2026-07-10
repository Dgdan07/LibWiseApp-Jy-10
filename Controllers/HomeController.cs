using Microsoft.AspNetCore.Mvc;

namespace LibWiseApp.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return RedirectToAction("Login", "Account");
    }

    public IActionResult Error()
    {
        return View("Error");
    }

    public new IActionResult StatusCode(int statusCode)
    {
        if (statusCode == 404)
            return View("NotFound");

        if (statusCode == 403)
            return View("Forbidden");

        return View("Error");
    }
}
