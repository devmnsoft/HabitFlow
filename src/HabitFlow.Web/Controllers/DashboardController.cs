using HabitFlow.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize]
public class DashboardController(HabitService habitService, ProgressService progressService, ILogger<DashboardController> logger) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        try
        {
            var user = this.CurrentUserSnapshot();
            var habits = await habitService.ListAsync(user.Id, ct);
            var dto = new DashboardDto(user.Name, habits.Count(x => !x.IsArchived), 0, 0, 0, habits.Select(x => new HabitDto(x.Id, x.Name, x.Color, x.Category, false, x.IsArchived)).ToList());
            return View(dto);
        }
        catch (Exception ex) { logger.LogError(ex, "Erro ao carregar dashboard"); TempData["Error"] = "Não foi possível carregar o dashboard."; return View(); }
    }
}
