using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize(Roles = "Admin")]
public class SettingsController(ILogger<SettingsController> logger) : Controller
{
    public IActionResult Index()
    {
        try { return RedirectToAction("Settings", "Admin"); }
        catch (Exception ex) { logger.LogError(ex, "Erro ao redirecionar configurações"); return RedirectToAction("Index", "Admin"); }
    }
}
