using HabitFlow.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize(Roles = "Admin")]
[Route("admin/database")]
public sealed class AdminDatabaseController(DatabaseDiagnosticsService diagnostics) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct) => View("~/Views/Admin/Database.cshtml", await diagnostics.GetAsync(ct));

    [HttpPost("validate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Validate(CancellationToken ct)
    {
        var result = await diagnostics.GetAsync(ct);
        TempData[result.Value?.Status == "healthy" ? "Success" : "Error"] = result.Value?.Status == "healthy"
            ? "Schema habitflow validado com sucesso."
            : "Validação do banco retornou alerta ou falha. Consulte os detalhes.";
        return View("~/Views/Admin/Database.cshtml", result);
    }
}
