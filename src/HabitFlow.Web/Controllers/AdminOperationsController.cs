using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize(Policy = "RequireAdmin")]
[Route("admin")]
public sealed class AdminOperationsController(ClientOnboardingService onboarding, ClientCommunicationService communications, CurrentUserContext currentUser) : Controller
{
    private Guid ClientId => currentUser.ClientId ?? Guid.Empty;
    [HttpGet("onboarding")]
    public async Task<IActionResult> Onboarding(CancellationToken ct) => View("~/Views/Admin/Onboarding.cshtml", ClientOnboardingService.BuildChecklist(await onboarding.GetOrCreateAsync(ClientId, ct)));
    [HttpPost("onboarding/company")][ValidateAntiForgeryToken] public async Task<IActionResult> CompanyStep(CancellationToken ct){ await onboarding.CompleteCompanyAsync(ClientId, ct); TempData["Success"]="Dados da conta confirmados."; return RedirectToAction(nameof(Onboarding)); }
    [HttpPost("onboarding/users")][ValidateAntiForgeryToken] public async Task<IActionResult> UsersStep(CancellationToken ct){ await onboarding.CompleteUsersAsync(ClientId, ct); return RedirectToAction(nameof(Onboarding)); }
    [HttpPost("onboarding/billing")][ValidateAntiForgeryToken] public async Task<IActionResult> BillingStep(CancellationToken ct){ await onboarding.CompleteBillingAsync(ClientId, ct); await onboarding.CompletePlanAsync(ClientId, ct); return RedirectToAction(nameof(Onboarding)); }
    [HttpPost("onboarding/finish")][ValidateAntiForgeryToken] public async Task<IActionResult> Finish(CancellationToken ct){ await onboarding.FinishAsync(ClientId, ct); TempData["Success"]="Tudo pronto. Sua conta já está preparada para uso."; return RedirectToAction(nameof(Onboarding)); }
    [HttpGet("company")] public IActionResult Company() => View("~/Views/Admin/Company.cshtml");
    [HttpPost("company/update")][ValidateAntiForgeryToken] public async Task<IActionResult> UpdateCompany(CancellationToken ct){ await onboarding.CompleteCompanyAsync(ClientId, ct); TempData["Success"]="Minha Empresa atualizada com segurança."; return RedirectToAction(nameof(Company)); }
    [HttpPost("company/billing-data")][ValidateAntiForgeryToken] public async Task<IActionResult> BillingData(CancellationToken ct){ await onboarding.CompleteBillingAsync(ClientId, ct); TempData["Success"]="Dados de cobrança registrados sem dados sensíveis."; return RedirectToAction(nameof(Company)); }
    [HttpGet("communications")] public async Task<IActionResult> Communications(string? type, string? status, CancellationToken ct) => View("~/Views/Admin/Communications.cshtml", await communications.ListByClientAsync(ClientId, new ClientCommunicationFilter(Type: type, Status: status), ct));
}
