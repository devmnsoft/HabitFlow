using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize(Roles = "Admin")]
public sealed class AdminHabitLibraryController(IHabitObjectiveRepository objectives, IHabitTemplateRepository templates, AdminAuditService adminAudit, AuditService audit) : Controller
{
    [HttpGet("/admin/habit-library")]
    public async Task<IActionResult> Index(CancellationToken ct) => View("~/Views/Admin/HabitLibrary.cshtml", (Objectives: await objectives.ListAllForAdminAsync(ct), Templates: await templates.ListAllForAdminAsync(ct)));

    [ValidateAntiForgeryToken]
    [HttpPost("/admin/habit-library/objective/toggle")]
    public async Task<IActionResult> ToggleObjective(Guid id, bool isActive, CancellationToken ct)
    {
        await objectives.ToggleActiveAsync(id, isActive, ct);
        await adminAudit.LogAsync(this.CurrentUserSnapshot(), "admin_habit_objective_toggled", "Alteração de status de objetivo da biblioteca", id, null, ct);
        await audit.LogAsync("admin_habit_objective_toggled", "Admin alterou status de objetivo da biblioteca", AuditSeverity.Warning, this.CurrentUserId(), User.Identity?.Name, new { id, isActive }, ct);
        TempData["Success"] = "Objetivo atualizado.";
        return RedirectToAction(nameof(Index));
    }

    [ValidateAntiForgeryToken]
    [HttpPost("/admin/habit-library/template/toggle")]
    public async Task<IActionResult> ToggleTemplate(Guid id, bool isActive, CancellationToken ct)
    {
        await templates.ToggleActiveAsync(id, isActive, ct);
        await adminAudit.LogAsync(this.CurrentUserSnapshot(), "admin_habit_template_toggled", "Alteração de status de template da biblioteca", id, null, ct);
        await audit.LogAsync("admin_habit_template_toggled", "Admin alterou status de template da biblioteca", AuditSeverity.Warning, this.CurrentUserId(), User.Identity?.Name, new { id, isActive }, ct);
        TempData["Success"] = "Template atualizado.";
        return RedirectToAction(nameof(Index));
    }
}
