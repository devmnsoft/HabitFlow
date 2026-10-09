using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HabitFlow.Web.Controllers;

[Authorize(Roles = "Admin,TenantAdmin,TenantOwner,SuperAdmin")]
[Route("tenant-ai")]
public sealed class TenantAiController(
    IOptions<AssistantOptions> assistantOptions,
    IOptions<AiOptions> aiOptions,
    PlanEntitlementService planEntitlement,
    AiAdminService adminService,
    ILogger<TenantAiController> logger) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var clientId = this.CurrentClientId();
        var planCode = await planEntitlement.GetEffectivePlanAsync(clientId, ct);
        var canUseAi = await planEntitlement.CanUseFeatureAsync(this.CurrentUserId(), PlanFeatureCodes.AiAssistant, ct);

        ViewBag.EffectivePlan = planCode;
        ViewBag.CanUseAi = canUseAi;
        ViewBag.GroqAllowed = aiOptions.Value.Providers.Groq.Enabled;
        ViewBag.GeminiAllowed = aiOptions.Value.Providers.Gemini.Enabled;
        ViewBag.DeepSeekAllowed = aiOptions.Value.Providers.DeepSeek.Enabled;

        return View("~/Views/TenantAi/Index.cshtml", assistantOptions.Value);
    }

    [HttpPost("update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(bool enabled, string? provider, string? model, int? maxTokens, CancellationToken ct)
    {
        var clientId = this.CurrentClientId();
        var userId = this.CurrentUserId();

        logger.LogInformation("tenant.ai.configured ClientId={ClientId} UserId={UserId} Enabled={Enabled} Provider={Provider}",
            clientId, userId, enabled, provider);

        await adminService.RecordAsync(clientId, userId, provider ?? "Disabled", model ?? "", "Updated", "ui.tenant.ai.updated", HttpContext.TraceIdentifier, 0, ct);

        TempData["Success"] = "Configurações de IA do Tenant atualizadas com sucesso para a sessão atual.";
        return RedirectToAction(nameof(Index));
    }
}
