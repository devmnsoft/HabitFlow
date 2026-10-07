using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize(Roles = "Admin,SuperAdmin,TenantAdmin,TenantOwner")]
public sealed class AdminHabitLibraryController(IHabitObjectiveRepository objectives, IHabitTemplateRepository templates, AdminAuditService adminAudit, AuditService audit, CurrentUserContext currentUser) : Controller
{
    [HttpGet("/admin/habit-library")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var all = await templates.ListAllForAdminAsync(ct);
        var visible = currentUser.IsSuperAdmin ? all : all.Where(template => template.ClientId is null || template.ClientId == currentUser.ClientId).ToList();
        return View("~/Views/Admin/HabitLibrary.cshtml", (Objectives: await objectives.ListAllForAdminAsync(ct), Templates: visible));
    }

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
        var template = await templates.GetAsync(id, ct);
        var decision = HabitTemplateAccess.CanEdit(currentUser.IsSuperAdmin, template?.ClientId, currentUser.ClientId);
        if (template is null || !decision.Allowed)
        {
            TempData["Error"] = template is null ? "Template não encontrado." : decision.Message;
            return RedirectToAction(nameof(Index));
        }
        await templates.ToggleActiveAsync(id, isActive, ct);
        await adminAudit.LogAsync(this.CurrentUserSnapshot(), "admin_habit_template_toggled", "Alteração de status de template da biblioteca", id, null, ct);
        await audit.LogAsync("admin_habit_template_toggled", "Admin alterou status de template da biblioteca", AuditSeverity.Warning, this.CurrentUserId(), User.Identity?.Name, new { id, isActive }, ct);
        TempData["Success"] = "Template atualizado.";
        return RedirectToAction(nameof(Index));
    }
}
