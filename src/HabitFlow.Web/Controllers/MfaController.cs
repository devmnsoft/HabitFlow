using System.Security.Claims;
using HabitFlow.Application;
using HabitFlow.Domain;
using HabitFlow.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize]
public sealed class MfaController(IUserMfaRepository repository, IUserRepository users, TotpEnrollmentService enrollment,
    TotpSecretProtector protector, TotpValidationService validator, RecoveryCodeService recoveryCodes,
    MfaChallengeService challenges, TimeProvider clock) : Controller
{
    [HttpGet("/account/security/mfa")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var setting = await repository.GetAsync(this.CurrentUserId(), this.CurrentClientIdOrNull(), ct);
        return View(new MfaSettingsViewModel(setting?.IsEnabled == true, setting?.EnabledAt));
    }

    [ValidateAntiForgeryToken, HttpPost("/account/security/mfa/start")]
    public async Task<IActionResult> Start(CancellationToken ct)
    {
        var user = await users.GetByIdAsync(this.CurrentUserId(), ct);
        if (user is null) return Challenge();
        var setup = await enrollment.StartAsync(user.Id, user.ClientId, user.Email, ct);
        return View("Index", new MfaSettingsViewModel(false, null, setup.ManualKey, setup.OtpAuthUri));
    }

    [ValidateAntiForgeryToken, HttpPost("/account/security/mfa/confirm")]
    public async Task<IActionResult> Confirm(MfaCodeViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return await Index(ct);
        var userId = this.CurrentUserId(); var clientId = this.CurrentClientIdOrNull();
        var setting = await repository.GetAsync(userId, clientId, ct);
        if (setting is null || setting.IsEnabled || !validator.TryValidate(protector.Unprotect(setting.ProtectedSecret), model.Code, out var step) ||
            !await repository.EnableAsync(userId, clientId, step, clock.GetUtcNow().UtcDateTime, ct))
        { ModelState.AddModelError(nameof(model.Code), "Código inválido ou expirado."); return await Index(ct); }
        var codes = await recoveryCodes.RegenerateAsync(userId, clientId, ct);
        await repository.AddSecurityEventAsync(userId, clientId, "MfaEnabled", clock.GetUtcNow().UtcDateTime, ct);
        return View("Index", new MfaSettingsViewModel(true, clock.GetUtcNow().UtcDateTime, RecoveryCodes: codes));
    }

    [ValidateAntiForgeryToken, HttpPost("/account/security/mfa/disable")]
    public async Task<IActionResult> Disable(MfaCodeViewModel model, CancellationToken ct)
    {
        var userId = this.CurrentUserId(); var clientId = this.CurrentClientIdOrNull();
        var setting = await repository.GetAsync(userId, clientId, ct);
        if (!ModelState.IsValid || setting is null || !setting.IsEnabled || !validator.TryValidate(protector.Unprotect(setting.ProtectedSecret), model.Code, out var step) ||
            !await repository.AcceptTimeStepAsync(userId, clientId, step, ct))
        { TempData["Error"] = "Não foi possível validar o código."; return RedirectToAction(nameof(Index)); }
        await repository.DisableAsync(userId, clientId, clock.GetUtcNow().UtcDateTime, ct);
        await repository.AddSecurityEventAsync(userId, clientId, "MfaDisabled", clock.GetUtcNow().UtcDateTime, ct);
        TempData["Success"] = "Autenticação em duas etapas desativada.";
        return RedirectToAction(nameof(Index));
    }

    [ValidateAntiForgeryToken, HttpPost("/account/security/mfa/recovery-codes")]
    public async Task<IActionResult> RecoveryCodes(MfaCodeViewModel model, CancellationToken ct)
    {
        var userId = this.CurrentUserId(); var clientId = this.CurrentClientIdOrNull();
        var setting = await repository.GetAsync(userId, clientId, ct);
        if (!ModelState.IsValid || setting is null || !setting.IsEnabled || !validator.TryValidate(protector.Unprotect(setting.ProtectedSecret), model.Code, out var step) ||
            !await repository.AcceptTimeStepAsync(userId, clientId, step, ct)) return RedirectToAction(nameof(Index));
        return View("Index", new MfaSettingsViewModel(true, setting.EnabledAt, RecoveryCodes: await recoveryCodes.RegenerateAsync(userId, clientId, ct)));
    }

    [HttpGet("/account/security/mfa/challenge")]
    public async Task<IActionResult> ChallengePage(CancellationToken ct)
    {
        var challenge = await challenges.StartAsync(this.CurrentUserId(), this.CurrentClientIdOrNull(), ct);
        return View("Challenge", new MfaChallengeViewModel { ChallengeId = challenge.Id });
    }

    [ValidateAntiForgeryToken, HttpPost("/account/security/mfa/challenge")]
    public async Task<IActionResult> ChallengeCode(MfaChallengeViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid || !await challenges.ValidateAsync(model.ChallengeId, this.CurrentUserId(), this.CurrentClientIdOrNull(), model.Code, ct))
        { ModelState.AddModelError(nameof(model.Code), "Código inválido. Após cinco tentativas, inicie novamente."); return View("Challenge", model); }
        await RenewPrincipalAsync();
        return LocalRedirect(Url.IsLocalUrl(Request.Query["returnUrl"]) ? Request.Query["returnUrl"]! : "/superadmin");
    }

    [ValidateAntiForgeryToken, HttpPost("/account/security/mfa/recovery-code")]
    public async Task<IActionResult> RecoveryChallenge(MfaChallengeViewModel model, CancellationToken ct)
    {
        var userId = this.CurrentUserId(); var clientId = this.CurrentClientIdOrNull();
        var challenge = await repository.GetChallengeAsync(model.ChallengeId, userId, clientId, ct);
        if (!ModelState.IsValid || challenge is null || challenge.VerifiedAt is not null || challenge.FailedAttempts >= 5 ||
            challenge.ExpiresAt <= clock.GetUtcNow().UtcDateTime || !await recoveryCodes.ConsumeAsync(userId, clientId, model.Code, ct))
        { ModelState.AddModelError(nameof(model.Code), "Código de recuperação inválido ou já utilizado."); return View("Challenge", model); }
        await repository.VerifyChallengeAsync(model.ChallengeId, userId, clientId, clock.GetUtcNow().UtcDateTime, ct);
        await repository.AddSecurityEventAsync(userId, clientId, "MfaRecoveryCodeUsed", clock.GetUtcNow().UtcDateTime, ct);
        await RenewPrincipalAsync();
        return Redirect("/superadmin");
    }

    private async Task RenewPrincipalAsync()
    {
        var result = await HttpContext.AuthenticateAsync();
        if (result.Principal?.Identity is ClaimsIdentity identity && !identity.HasClaim(x => x.Type == "mfa_verified")) identity.AddClaim(new Claim("mfa_verified", "true"));
        if (result.Principal is not null) await HttpContext.SignInAsync(result.Principal, result.Properties);
    }
}
