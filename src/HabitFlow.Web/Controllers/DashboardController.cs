using HabitFlow.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize]
public class DashboardController(HabitService habitService, ILogger<DashboardController> logger, CurrentUserContext currentUser, ClientOnboardingService onboarding) : Controller
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
            var user = this.CurrentUserSnapshot();
            var habits = await habitService.ListAsync(user.Id, ct);
            var dto = new DashboardDto(user.Name, habits.Count(x => !x.IsArchived), 0, 0, 0, habits.Select(x => new HabitDto(x.Id, x.Name, x.Color, x.Category, false, x.IsArchived)).ToList());
            return View(dto);
        }
        catch (Exception ex) { logger.LogError(ex, "Erro ao carregar dashboard"); TempData["Error"] = "Não foi possível carregar o dashboard."; return View(); }
    }
}
