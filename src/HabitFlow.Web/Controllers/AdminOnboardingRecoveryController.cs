using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

// Página de recuperação para usuários autenticados sem vínculo com cliente
// (ClientBindingMiddleware redireciona para /admin/onboarding/recover-client).
// Mantida em controller próprio com [Authorize] simples — a rota pertence ao fluxo
// de onboarding, mas o usuário afetado pode não ser admin do tenant.
[Authorize]
[Route("admin/onboarding")]
public sealed class AdminOnboardingRecoveryController : Controller
{
    [HttpGet("recover-client")]
    public IActionResult RecoverClient() => View("~/Views/Admin/RecoverClient.cshtml");
}
