using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

public class AccountController : Controller
{
    // Página legível de acesso negado: substitui a tela em branco (404 com body vazio)
    // para quem é Forbid() sem permissão. ReturnUrl chega no query string via
    // OnRedirectToAccessDenied e só é exibida/linkada quando for URL local.
    [HttpGet("/account/access-denied")]
    public IActionResult AccessDenied(string? returnUrl) =>
        View("~/Views/Shared/AccessDenied.cshtml", returnUrl);
}
