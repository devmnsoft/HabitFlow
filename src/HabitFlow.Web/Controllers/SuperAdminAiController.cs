using System.Text.RegularExpressions;
using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize(Roles = "SuperAdmin")]
[Route("superadmin/ai")]
public sealed class SuperAdminAiController(AiAdminService admin) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        await LoadLists(null, ct);
        return View(await Settings(ct));
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(bool enabled, string? defaultProvider, bool groqEnabled, bool geminiEnabled, bool deepSeekEnabled, string? groqModels, string? geminiModels, string? deepSeekModels, int? globalDailyLimit, int? freeLimit, int? ritmoLimit, int? teamLimit, int? enterpriseLimit, CancellationToken ct)
    {
        var settings = new AiRuntimeSettings(enabled, (defaultProvider ?? "").Trim(), groqEnabled, geminiEnabled, deepSeekEnabled, Clean(groqModels), Clean(geminiModels), Clean(deepSeekModels), globalDailyLimit ?? -1, DateTime.UtcNow);
        var limits = new AiPlanLimit[] { new("free", freeLimit ?? -1), new("ritmo", ritmoLimit ?? -1), new("team", teamLimit ?? -1), new("enterprise", enterpriseLimit ?? -1) };
        if (globalDailyLimit is null || freeLimit is null || ritmoLimit is null || teamLimit is null || enterpriseLimit is null)
            ModelState.AddModelError("limits", "Preencha este campo obrigatório.");
        var error = ModelState.IsValid ? await admin.SaveAsync(settings, limits, this.CurrentUserId(), ct) : "Revise os campos indicados.";
        if (!ModelState.IsValid || error is not null)
        {
            if (error is not null) ModelState.AddModelError(string.Empty, error);
            await admin.RecordAsync(this.CurrentClientId(), this.CurrentUserId(), defaultProvider ?? "", "", "Invalid", "ui.form.validation.failed", HttpContext.TraceIdentifier, 0, ct);
            ViewBag.Limits = limits;
            await LoadLists(null, ct);
            return View("Index", settings);
        }
        await admin.RecordAsync(this.CurrentClientId(), this.CurrentUserId(), settings.DefaultProvider, "", "Updated", "ai.settings.updated", HttpContext.TraceIdentifier, 0, ct);
        await admin.RecordAsync(this.CurrentClientId(), this.CurrentUserId(), settings.DefaultProvider, "", "Audited", "security.admin_action_audited", HttpContext.TraceIdentifier, 0, ct);
        TempData["Success"] = "Configuração de IA aplicada. Chaves continuam só nas variáveis de ambiente.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<AiRuntimeSettings> Settings(CancellationToken ct)
    {
        try { return await admin.SettingsAsync(ct); }
        catch { return new(false, "", false, false, false, "", "", "", 200, DateTime.UtcNow); }
    }

    private async Task LoadLists(Guid? clientId, CancellationToken ct)
    {
        ViewBag.GroqKey = AiAdminService.HasKey("Groq");
        ViewBag.GeminiKey = AiAdminService.HasKey("Gemini");
        ViewBag.DeepSeekKey = AiAdminService.HasKey("DeepSeek");
        try { ViewBag.Limits ??= await admin.PlanLimitsAsync(ct); } catch { ViewBag.Limits ??= Array.Empty<AiPlanLimit>(); }
        try { ViewBag.Recent = await admin.RecentAsync(clientId, ct); } catch { ViewBag.Recent = Array.Empty<AiUsageEventRow>(); }
        try { ViewBag.Failures = await admin.FailuresAsync(clientId, ct); } catch { ViewBag.Failures = Array.Empty<AiUsageEventRow>(); }
        try { ViewBag.Consumption = await admin.ConsumptionAsync(clientId, ct); } catch { ViewBag.Consumption = Array.Empty<AiTenantConsumption>(); }
    }

    private static string Clean(string? value) => Regex.Replace(value ?? "", @"\s+", "");
}
