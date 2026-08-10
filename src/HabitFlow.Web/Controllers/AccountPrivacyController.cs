using HabitFlow.Domain;
using HabitFlow.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize]
[Route("account/privacy")]
public sealed class AccountPrivacyController(AccountPrivacyService privacy, UserFeedbackService feedback, ILogger<AccountPrivacyController> logger) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (!HasBoundIdentity()) return Forbid();
        try { return View(await privacy.GetAsync(this.CurrentUserSnapshot(), ct)); }
        catch (Exception ex) { logger.LogError(ex, "Falha ao carregar central de privacidade para {UserId}", this.CurrentUserId()); feedback.Error(this, "A Central de Privacidade está temporariamente indisponível. Tente novamente mais tarde."); return View("Index", null); }
    }

    [HttpPost("consents"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Consents(string key, bool granted, CancellationToken ct)
    {
        if (!HasBoundIdentity()) return Forbid();
        try { await privacy.SetConsentAsync(this.CurrentUserId(), key, granted, ct); feedback.Success(this, "Sua preferência de privacidade foi salva."); }
        catch (ArgumentException) { feedback.Error(this, "A preferência informada não é válida."); }
        catch (Exception ex) { logger.LogError(ex, "Falha ao salvar consentimento para {UserId}", this.CurrentUserId()); feedback.Error(this, "Não foi possível salvar sua preferência agora."); }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("export-request"), ValidateAntiForgeryToken] public Task<IActionResult> Export(CancellationToken ct) => Register(LgpdRequestType.Export, "Sua exportação entrou na fila para análise segura.", ct);
    [HttpPost("delete-request"), ValidateAntiForgeryToken] public Task<IActionResult> Delete(CancellationToken ct) => Register(LgpdRequestType.Delete, "Sua solicitação de exclusão foi registrada e será analisada. Nenhum dado foi excluído agora.", ct);
    [HttpPost("anonymization-request"), ValidateAntiForgeryToken] public Task<IActionResult> Anonymize(CancellationToken ct) => Register(LgpdRequestType.Anonymize, "Sua solicitação de anonimização foi registrada para análise.", ct);

    private async Task<IActionResult> Register(LgpdRequestType type, string success, CancellationToken ct)
    {
        if (!HasBoundIdentity()) return Forbid();
        var result = await privacy.RequestAsync(this.CurrentUserSnapshot(), type, ct);
        if (result.IsSuccess) feedback.Success(this, success); else feedback.Error(this, "Não foi possível registrar a solicitação. Tente novamente mais tarde.");
        return RedirectToAction(nameof(Index));
    }
    private bool HasBoundIdentity() => this.CurrentUserId() != Guid.Empty && this.CurrentClientId() != Guid.Empty;
}
