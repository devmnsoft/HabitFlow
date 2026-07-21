using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

public sealed class DemoController : Controller
{
    [HttpGet("demo")]
    public IActionResult Index() => View();
}
