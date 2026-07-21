using HabitFlow.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController(AdminService adminService, SettingsService settingsService, ILogger<AdminController> logger) : Controller
{
    public IActionResult Index() => View();

    public async Task<IActionResult> Users(string? q, CancellationToken ct)
    {
        try { return View(await adminService.SearchUsersAsync(this.CurrentUserSnapshot(), q, ct)); }
        catch (Exception ex) { logger.LogError(ex, "Erro ao carregar usuários no admin"); TempData["Error"] = "Não foi possível carregar usuários."; return View(); }
    }

    public IActionResult UserDetail() => View();

    public IActionResult Logs() => View();

    public async Task<IActionResult> Settings(CancellationToken ct)
    {
        try { return View(await settingsService.ListAsync(ct)); }
        catch (Exception ex) { logger.LogError(ex, "Erro ao carregar configurações no admin"); TempData["Error"] = "Não foi possível carregar configurações."; return View(); }
    }
}
