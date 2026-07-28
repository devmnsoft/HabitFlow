using System.Security.Cryptography;
using System.Text;
using HabitFlow.Application;
using HabitFlow.Domain;
using HabitFlow.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[AllowAnonymous]
public sealed class PasswordRecoveryController(PasswordRecoveryService recovery, PasswordResetService reset) : Controller
{
    private const string InvalidLink = "Este link não está mais disponível. Solicite uma nova recuperação de senha.";

    [HttpGet("/forgot-password")]
    public IActionResult ForgotPassword() { SecureResponse(); return View(new ForgotPasswordViewModel()); }

    [ValidateAntiForgeryToken]
    [HttpPost("/forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model, CancellationToken ct)
    {
        SecureResponse();
        if (ModelState.IsValid)
        {
            var ipHash = Hash(HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
            var uaHash = Hash(Request.Headers.UserAgent.ToString());
            await recovery.RequestAsync(new PasswordResetRequest(model.Email, ipHash, uaHash, HttpContext.TraceIdentifier), ct);
        }
        // Invalid formatting follows the same public destination to avoid an enumeration side channel.
        return RedirectToAction(nameof(ForgotPasswordSent));
    }

    [HttpGet("/forgot-password/sent")]
    public IActionResult ForgotPasswordSent() { SecureResponse(); return View(); }

    [HttpGet("/reset-password")]
    public async Task<IActionResult> ResetPassword([FromQuery] string? token, CancellationToken ct)
    {
        SecureResponse();
        if (string.IsNullOrWhiteSpace(token) || !(await reset.ValidateAsync(token, ct)).IsValid) ViewData["InvalidLink"] = InvalidLink;
        return View(new ResetPasswordViewModel { Token = token ?? "" });
    }

    [ValidateAntiForgeryToken]
    [HttpPost("/reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model, CancellationToken ct)
    {
        SecureResponse();
        if (!ModelState.IsValid) return View(model);
        var result = await reset.ResetAsync(model.Token, model.NewPassword, ct);
        if (!result.Succeeded) { ModelState.AddModelError(string.Empty, result.Error ?? InvalidLink); return View(model); }
        return RedirectToAction(nameof(ResetPasswordSuccess));
    }

    [HttpGet("/reset-password/success")]
    public IActionResult ResetPasswordSuccess() { SecureResponse(); return View(); }

    private void SecureResponse()
    {
        Response.Headers.CacheControl = "no-store, no-cache, max-age=0";
        Response.Headers.Pragma = "no-cache";
        Response.Headers["Referrer-Policy"] = "no-referrer";
    }
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
