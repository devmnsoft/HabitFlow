using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

public class HomeController(ILogger<HomeController> logger) : Controller
{
    public IActionResult Index() => View();

    public IActionResult Error()
    {
        logger.LogWarning("Página de erro amigável exibida");
        return View();
    }
}
