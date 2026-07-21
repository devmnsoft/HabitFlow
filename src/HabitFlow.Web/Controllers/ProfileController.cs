using HabitFlow.Application;
using HabitFlow.Domain;
using HabitFlow.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize]
public class ProfileController(ProfileService profileService, UserUiPreferenceService uiPreferenceService, ILogger<ProfileController> logger) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        try { return View(await profileService.GetAsync(this.CurrentUserId(), ct)); }
        catch (Exception ex) { logger.LogError(ex, "Erro ao carregar perfil"); TempData["Error"] = "Não foi possível carregar o perfil."; return View(); }
    }

    [HttpGet("/profile/accessibility")]
    public async Task<IActionResult> Accessibility(CancellationToken ct)
    {
        var preference = await uiPreferenceService.GetForUserAsync(this.CurrentUserId(), ct);
        return View(new UserUiPreferenceViewModel { ContrastMode = preference.ContrastMode, FontScale = preference.FontScale, ReduceMotion = preference.ReduceMotion });
    }

    [HttpPost("/profile/accessibility")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Accessibility(UserUiPreferenceViewModel model, CancellationToken ct)
    {
        if (!Enum.IsDefined(model.ContrastMode) || !Enum.IsDefined(model.FontScale))
        {
            ModelState.AddModelError(string.Empty, "Preferência visual inválida.");
            return View(model);
        }

        await uiPreferenceService.SaveAsync(this.CurrentUserId(), model.ContrastMode, model.FontScale, model.ReduceMotion, ct);
        TempData["Success"] = "Preferências de visualização salvas.";
        return RedirectToAction(nameof(Accessibility));
    }
}
