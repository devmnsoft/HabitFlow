using Microsoft.AspNetCore.Mvc;
namespace HabitFlow.Web.Controllers;
public sealed class LegalController : Controller
{
    [HttpGet("privacy")] public IActionResult Privacy() => View();
    [HttpGet("terms")] public IActionResult Terms() => View();
    [HttpGet("lgpd")] public IActionResult Lgpd() => View();
}
