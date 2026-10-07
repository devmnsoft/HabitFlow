using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize(Policy = "RequireAdmin")]
public sealed class HabitTemplateGovernanceController(
    IHabitTemplateRepository templates,
    IHabitObjectiveRepository objectives,
    PlanEntitlementService entitlements,
    CurrentUserContext currentUser) : Controller
{
    [HttpGet("/admin/templates")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (!currentUser.ClientId.HasValue && !currentUser.IsSuperAdmin) return Forbid();
        var visible = currentUser.IsSuperAdmin
            ? await templates.ListAllForAdminAsync(ct)
            : await templates.ListActiveAsync(currentUser.ClientId, ct);
        ViewData["CanOwn"] = currentUser.IsSuperAdmin || HabitTemplateAccess.CanOwnTemplates(await PlanAsync(ct));
        ViewData["Objectives"] = await objectives.ListActiveAsync(ct);
        return View("~/Views/Admin/Templates.cshtml", visible);
    }

    [HttpPost("/admin/templates")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TemplateForm form, CancellationToken ct)
    {
        if (!currentUser.ClientId.HasValue || currentUser.UserId == Guid.Empty) return Forbid();
        var plan = await PlanAsync(ct);
        if (!currentUser.IsSuperAdmin && !HabitTemplateAccess.CanOwnTemplates(plan))
        {
            TempData["Error"] = "Seu plano não permite templates próprios. Use a biblioteca global.";
            return RedirectToAction(nameof(Index));
        }
        var error = Validate(form);
        if (error is not null) { TempData["Error"] = error; return RedirectToAction(nameof(Index)); }
        var objective = await objectives.GetBySlugAsync(form.ObjectiveSlug, ct);
        if (objective is null) { TempData["Error"] = "Escolha um objetivo da lista."; return RedirectToAction(nameof(Index)); }
        await templates.CreateAsync(new HabitTemplateDraft(Guid.NewGuid(), objective.Id, currentUser.IsSuperAdmin && form.Global && currentUser.IsSuperAdmin ? null : currentUser.ClientId, currentUser.UserId, form.Name.Trim(), form.Description.Trim(), form.Category, form.Frequency, form.Difficulty, form.Minutes, form.Audience.Trim(), form.Goal.Trim(), form.MinimumPlan), ct);
        TempData["Success"] = "Template salvo.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("/superadmin/templates")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateGlobal(TemplateForm form, CancellationToken ct)
    {
        if (!currentUser.IsSuperAdmin) return Forbid();
        var error = Validate(form);
        if (error is not null) { TempData["Error"] = error; return Redirect("/admin/templates"); }
        var objective = await objectives.GetBySlugAsync(form.ObjectiveSlug, ct);
        if (objective is null) { TempData["Error"] = "Escolha um objetivo da lista."; return Redirect("/admin/templates"); }
        await templates.CreateAsync(new HabitTemplateDraft(Guid.NewGuid(), objective.Id, null, currentUser.UserId, form.Name.Trim(), form.Description.Trim(), form.Category, form.Frequency, form.Difficulty, form.Minutes, form.Audience.Trim(), form.Goal.Trim(), form.MinimumPlan), ct);
        TempData["Success"] = "Template global salvo.";
        return Redirect("/admin/templates");
    }

    private async Task<string> PlanAsync(CancellationToken ct)
    {
        if (currentUser.UserId == Guid.Empty) return PlanCodes.Free;
        return await entitlements.GetEffectivePlanForUserAsync(currentUser.UserId, ct);
    }

    private static string? Validate(TemplateForm form)
    {
        if (form.Name?.Trim().Length is < 2 or > 120) return "Informe um nome entre 2 e 120 caracteres.";
        if (form.Description?.Trim().Length is < 10 or > 300) return "Informe uma descrição entre 10 e 300 caracteres.";
        if (!HabitTemplateAccess.Categories.Contains(form.Category ?? "")) return "Escolha uma categoria da lista.";
        if (form.Frequency is not ("Daily" or "Weekdays" or "Weekends" or "CustomWeekly")) return "Escolha uma frequência da lista.";
        if (form.Difficulty is not ("Easy" or "Medium" or "Hard")) return "Escolha uma dificuldade da lista.";
        if (form.Minutes is < 1 or > 240) return "A duração estimada deve ficar entre 1 e 240 minutos.";
        if (form.Audience?.Trim().Length is < 2 or > 160) return "Informe o público indicado.";
        if (form.Goal?.Trim().Length is < 2 or > 300) return "Informe o objetivo do template.";
        if (form.MinimumPlan is not ("free" or "ritmo" or "team" or "enterprise")) return "Escolha o plano mínimo da lista.";
        if (string.IsNullOrWhiteSpace(form.ObjectiveSlug)) return "Escolha o objetivo da lista.";
        return null;
    }

    public sealed class TemplateForm
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Category { get; set; } = "";
        public string Frequency { get; set; } = "Daily";
        public string Difficulty { get; set; } = "Easy";
        public int Minutes { get; set; } = 10;
        public string Audience { get; set; } = "";
        public string Goal { get; set; } = "";
        public string MinimumPlan { get; set; } = "free";
        public string ObjectiveSlug { get; set; } = "";
        public bool Global { get; set; }
    }
}
