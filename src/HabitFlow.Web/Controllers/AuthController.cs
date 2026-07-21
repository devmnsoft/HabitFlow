using System.Security.Claims;
using HabitFlow.Application;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

public class AuthController(AuthService authService, IWebHostEnvironment env, ILogger<AuthController> logger) : Controller
{
    [HttpGet("/login")]
    public IActionResult Login() => View();

    [ValidateAntiForgeryToken]
    [HttpPost("/login")]
    public async Task<IActionResult> Login(LoginDto dto, CancellationToken ct)
    {
        try
        {
            if (!ModelState.IsValid) return View(dto);
            var result = await authService.LoginAsync(dto, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent, ct);
            if (result.IsFailure) { SetFailureMessage(result.Error.Code, result.Error.Message); return View(dto); }
            var user = result.Value!;
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("account_status", user.AccountStatus.ToString())
            }, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
            return RedirectToAction("Index", "Dashboard");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro inesperado no POST /login");
            TempData["Error"] = "Não foi possível concluir o login agora.";
            return View(dto);
        }
    }

    [HttpGet("/register")]
    public IActionResult Register() => View();

    [ValidateAntiForgeryToken]
    [HttpPost("/register")]
    public async Task<IActionResult> Register(RegisterDto dto, CancellationToken ct)
    {
        try
        {
            if (!ModelState.IsValid) return View(dto);
            var result = await authService.RegisterAsync(dto, ct);
            if (result.IsFailure) { SetFailureMessage(result.Error.Code, result.Error.Message); return View(dto); }
            TempData["Success"] = "Cadastro criado. Faça login para continuar.";
            return RedirectToAction(nameof(Login));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro inesperado no POST /register");
            TempData["Error"] = "Não foi possível concluir o cadastro agora.";
            return View(dto);
        }
    }

    private void SetFailureMessage(string code, string message)
    {
        if (code.StartsWith("postgres.", StringComparison.OrdinalIgnoreCase))
        {
            TempData["DatabaseError"] = message;
            TempData["DatabaseErrorCode"] = code;
            if (env.IsDevelopment()) TempData["Info"] = "Dica para desenvolvimento: acesse /health/db para ver o diagnóstico.";
        }
        else if (code.StartsWith("validation.", StringComparison.OrdinalIgnoreCase))
        {
            TempData["ValidationError"] = message;
        }
        else
        {
            TempData["Error"] = message;
        }
    }

    [ValidateAntiForgeryToken]
    [HttpPost("/logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        try { await HttpContext.SignOutAsync(); return RedirectToAction("Index", "Home"); }
        catch (Exception ex) { logger.LogError(ex, "Erro ao sair"); TempData["Error"] = "Não foi possível encerrar a sessão."; return RedirectToAction("Index", "Dashboard"); }
    }
}
