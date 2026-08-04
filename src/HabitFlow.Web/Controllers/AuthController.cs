using System.Security.Claims;
using HabitFlow.Application;
using HabitFlow.Domain;
using HabitFlow.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

public class AuthController(AuthService authService, ClientAccountRegistrationService clientRegistration, UserSessionService sessionService, IConfiguration configuration, IWebHostEnvironment env, IUserFacingErrorMapper errorMapper, ILogger<AuthController> logger) : Controller
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
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Name),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role.ToString()),
                new("account_status", user.AccountStatus.ToString())
                ,new("session_version", user.SessionVersion.ToString(System.Globalization.CultureInfo.InvariantCulture))
                ,new("must_change_password", user.MustChangePassword ? "true" : "false")
            };
            if (user.ClientId.HasValue)
            {
                claims.Add(new Claim("client_id", user.ClientId.Value.ToString()));
            }
            var lifetime = TimeSpan.FromHours(configuration.GetValue("Authentication:CookieHours", 8));
            var sessionId = await sessionService.StartAsync(user.Id, user.ClientId, Request.Headers.UserAgent, HttpContext.Connection.RemoteIpAddress?.ToString(), lifetime, ct);
            claims.Add(new Claim("session_id", sessionId.ToString()));
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
            if (user.MustChangePassword) return Redirect("/account/security/change-required-password");
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
    public IActionResult Register() => View(new RegisterViewModel());

    [ValidateAntiForgeryToken]
    [HttpPost("/register")]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken ct)
    {
        try
        {
            if (!ModelState.IsValid) return View(model);
            var result = await clientRegistration.RegisterAsync(model.ToClientAccountDto(), ct);
            if (result.IsFailure)
            {
                if (result.Error.Code == "validation.cpf_invalid" || result.Error.Code == "validation.cnpj_invalid") ModelState.AddModelError(nameof(RegisterViewModel.Document), result.Error.Message);
                else if (result.Error.Code == "validation.document_duplicate") TempData["Warning"] = "Conta já existente: Já existe uma conta cadastrada com este CPF/CNPJ.";
                else SetFailureMessage(result.Error.Code, result.Error.Message);
                return View(model);
            }
            TempData["Success"] = "Conta criada com sucesso. Entre para concluir a configuração da sua conta.";
            return RedirectToAction(nameof(Login));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro inesperado no POST /register");
            TempData["Error"] = "Não foi possível concluir o cadastro agora.";
            return View(model);
        }
    }

    private void SetFailureMessage(string code, string message)
    {
        if (code.StartsWith("postgres.", StringComparison.OrdinalIgnoreCase))
        {
            TempData["DatabaseError"] = errorMapper.ToPublicMessage(code);
            if (env.IsDevelopment() || User?.IsInRole("Admin") == true) TempData["Info"] = errorMapper.ToAdminMessage(code, message) + " Acesse /diagnostics/database.";
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
        try
        {
            if (Guid.TryParse(User.FindFirstValue("session_id"), out var sessionId))
                await HttpContext.RequestServices.GetRequiredService<IUserSessionRepository>().RevokeAsync(sessionId, this.CurrentUserId(), "logout", ct);
            await HttpContext.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex) { logger.LogError(ex, "Erro ao sair"); TempData["Error"] = "Não foi possível encerrar a sessão."; return RedirectToAction("Index", "Dashboard"); }
    }
}
