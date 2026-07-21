using HabitFlow.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize]
public class ProfileController(ProfileService profileService, ILogger<ProfileController> logger) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        try { return View(await profileService.GetAsync(this.CurrentUserId(), ct)); }
        catch (Exception ex) { logger.LogError(ex, "Erro ao carregar perfil"); TempData["Error"] = "Não foi possível carregar o perfil."; return View(); }
    }
}
