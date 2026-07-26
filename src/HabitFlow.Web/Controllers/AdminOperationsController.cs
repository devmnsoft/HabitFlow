using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize(Policy = "RequireAdmin")]
[Route("admin")]
public sealed class AdminOperationsController(ClientOnboardingService onboarding, ClientCommunicationService communications, CurrentUserContext currentUser, ClientService clients) : Controller
{
    private Guid ClientId => currentUser.ClientId ?? Guid.Empty;
    [HttpGet("onboarding")]
    public async Task<IActionResult> Onboarding(CancellationToken ct)
    {
        var client = (await clients.GetByIdAsync(ClientId, ct)).Value?.Client;
        ViewData["OnboardingTitle"] = client?.PersonType == ClientPersonType.NaturalPerson ? "Complete sua conta" : "Complete os dados da empresa";
        return View("~/Views/Admin/Onboarding.cshtml", ClientOnboardingService.BuildChecklist(await onboarding.GetOrCreateAsync(ClientId, ct)));
    }
    [AllowAnonymous]
    [HttpGet("onboarding/recover-client")] public IActionResult RecoverClient() => View("~/Views/Admin/RecoverClient.cshtml");
    [HttpPost("onboarding/company")][ValidateAntiForgeryToken] public async Task<IActionResult> CompanyStep(CancellationToken ct){ await onboarding.CompleteCompanyAsync(ClientId, ct); TempData["Success"]="Dados da conta confirmados."; return RedirectToAction(nameof(Onboarding)); }
    [HttpPost("onboarding/users")][ValidateAntiForgeryToken] public async Task<IActionResult> UsersStep(CancellationToken ct){ await onboarding.CompleteUsersAsync(ClientId, ct); return RedirectToAction(nameof(Onboarding)); }
    [HttpPost("onboarding/billing")][ValidateAntiForgeryToken] public async Task<IActionResult> BillingStep(CancellationToken ct){ await onboarding.CompleteBillingAsync(ClientId, ct); await onboarding.CompletePlanAsync(ClientId, ct); return RedirectToAction(nameof(Onboarding)); }
    [HttpPost("onboarding/finish")][ValidateAntiForgeryToken] public async Task<IActionResult> Finish(CancellationToken ct){ await onboarding.FinishAsync(ClientId, ct); TempData["Success"]="Tudo pronto. Sua conta já está preparada para uso."; return RedirectToAction(nameof(Onboarding)); }
    [Authorize(Roles = "Admin")]
    [HttpGet("company")] public async Task<IActionResult> Company(CancellationToken ct)
    {
        var result = await clients.GetByIdAsync(ClientId, ct);
        if (result.IsFailure) { TempData["Error"] = "Não foi possível carregar os dados da conta."; return RedirectToAction(nameof(Onboarding)); }
        return View("~/Views/Admin/Company.cshtml", result.Value!.Client);
    }
    [HttpPost("company/update")][ValidateAntiForgeryToken] public async Task<IActionResult> UpdateCompany(CancellationToken ct){ await onboarding.CompleteCompanyAsync(ClientId, ct); TempData["Success"]="Minha Empresa atualizada com segurança."; return RedirectToAction(nameof(Company)); }
    [HttpPost("company/billing-data")][ValidateAntiForgeryToken] public async Task<IActionResult> BillingData(CancellationToken ct){ await onboarding.CompleteBillingAsync(ClientId, ct); TempData["Success"]="Dados de cobrança registrados sem dados sensíveis."; return RedirectToAction(nameof(Company)); }
    [HttpGet("communications")] public async Task<IActionResult> Communications(string? type, string? status, CancellationToken ct) => View("~/Views/Admin/Communications.cshtml", await communications.ListByClientAsync(ClientId, new ClientCommunicationFilter(Type: type, Status: status), ct));
}
