using HabitFlow.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController(AdminDashboardService dashboard, SettingsService settingsService, ILogger<AdminController> logger) : Controller
{
    [HttpGet("admin")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        try { return View(await dashboard.GetDashboardAsync(this.CurrentUserSnapshot(), ct)); }
        catch (Exception ex) { logger.LogError(ex, "Erro ao carregar dashboard admin"); TempData["Error"] = "Não foi possível carregar o dashboard."; return View(); }
    }

    public IActionResult Logs() => RedirectToAction("System", "AdminLogs");

    public async Task<IActionResult> Settings(CancellationToken ct)
    {
        try { return View(await settingsService.ListAsync(ct)); }
        catch (Exception ex) { logger.LogError(ex, "Erro ao carregar configurações no admin"); TempData["Error"] = "Não foi possível carregar configurações."; return View(); }
    }
}
