using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Route("help")]
public sealed class HelpController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();

    [HttpGet("getting-started")]
    public IActionResult GettingStarted() => View();

    [HttpGet("habits")]
    public IActionResult Habits() => View();

    [HttpGet("progress")]
    public IActionResult Progress() => View();

    [HttpGet("reports")]
    public IActionResult Reports() => View();

    [HttpGet("premium")]
    public IActionResult Premium() => View();

    [HttpGet("privacy")]
    public IActionResult Privacy() => View();

    [HttpGet("support")]
    public IActionResult Support() => View();

    [HttpGet("login")]
    public IActionResult Login() => View();

    [HttpGet("database-setup")]
    public IActionResult DatabaseSetup() => View();
}
