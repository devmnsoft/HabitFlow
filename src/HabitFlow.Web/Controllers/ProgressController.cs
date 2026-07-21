using HabitFlow.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize]
public class ProgressController(ILogger<ProgressController> logger) : Controller
{
    public IActionResult Index()
    {
        try { return View(new ProgressDto(0, 0, 0, 0, 0)); }
        catch (Exception ex) { logger.LogError(ex, "Erro ao carregar progresso"); return View(new ProgressDto(0, 0, 0, 0, 0)); }
    }
}
