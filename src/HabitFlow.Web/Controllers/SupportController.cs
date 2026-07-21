using HabitFlow.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize]
public class SupportController(SupportService supportService, ILogger<SupportController> logger) : Controller
{
    public IActionResult Index() => View();

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> Create(string title, string description, CancellationToken ct)
    {
        try { var result = await supportService.CreateTicketAsync(this.CurrentUserSnapshot(), title, description, ct); TempData[result.IsFailure ? "Error" : "Success"] = result.IsFailure ? result.Error.Message : "Chamado aberto."; }
        catch (Exception ex) { logger.LogError(ex, "Erro ao abrir suporte"); TempData["Error"] = "Não foi possível abrir o chamado."; }
        return RedirectToAction(nameof(Index));
    }
}
