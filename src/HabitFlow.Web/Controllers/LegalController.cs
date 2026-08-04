using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.AspNetCore.Mvc;
namespace HabitFlow.Web.Controllers;
public sealed class LegalController(LegalDocumentQueryService documents) : Controller
{
    [HttpGet("/privacy"), HttpGet("/legal/privacy")] public Task<IActionResult> Privacy(CancellationToken ct) => Document(LegalDocumentType.PrivacyNotice, nameof(Privacy), ct);
    [HttpGet("/terms"), HttpGet("/legal/terms")] public Task<IActionResult> Terms(CancellationToken ct) => Document(LegalDocumentType.TermsOfUse, nameof(Terms), ct);
    [HttpGet("/legal/cookies")] public Task<IActionResult> Cookies(CancellationToken ct) => Document(LegalDocumentType.CookieNotice, nameof(Cookies), ct);
    [HttpGet("/legal/health-disclaimer")] public Task<IActionResult> HealthDisclaimer(CancellationToken ct) => Document(LegalDocumentType.HealthDisclaimer, nameof(HealthDisclaimer), ct);
    [HttpGet("lgpd")] public IActionResult Lgpd() => View();

    private async Task<IActionResult> Document(LegalDocumentType type, string fallbackView, CancellationToken ct)
    {
        var published = await documents.PublishedAsync(type, ct);
        return published is null ? View(fallbackView) : View("Document", published);
    }
}
