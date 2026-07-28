using HabitFlow.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize]
public class DashboardController(TodayDashboardService dashboard, ILogger<DashboardController> logger, CurrentUserContext currentUser, ClientOnboardingService onboarding) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        try
        {
            if (currentUser.IsAdmin && currentUser.ClientId.HasValue)
            {
                var state = await onboarding.GetOrCreateAsync(currentUser.ClientId.Value, ct);
                if (!state.Completed) return RedirectToAction("Onboarding", "AdminOperations");
            }
            return View(await dashboard.BuildAsync(this.CurrentUserId(), ct));
        }
        catch (Exception ex) { logger.LogError(ex, "Erro ao carregar dashboard"); TempData["Error"] = "Não foi possível carregar o dashboard."; return View(); }
    }
}
