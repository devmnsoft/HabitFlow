using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.AspNetCore.Mvc;
namespace HabitFlow.Web.Controllers;
public sealed class LegalController(LegalDocumentQueryService documents, IConfiguration configuration, ILogger<LegalController> logger) : Controller
{
    [HttpGet("/privacy"), HttpGet("/legal/privacy")] public Task<IActionResult> Privacy(CancellationToken ct) => Document(LegalDocumentType.PrivacyNotice, nameof(Privacy), ct);
    [HttpGet("/terms"), HttpGet("/legal/terms")] public Task<IActionResult> Terms(CancellationToken ct) => Document(LegalDocumentType.TermsOfUse, nameof(Terms), ct);
    [HttpGet("/legal/cookies")] public Task<IActionResult> Cookies(CancellationToken ct) => Document(LegalDocumentType.CookieNotice, nameof(Cookies), ct);
    [HttpGet("/legal/health-disclaimer")] public Task<IActionResult> HealthDisclaimer(CancellationToken ct) => Document(LegalDocumentType.HealthDisclaimer, nameof(HealthDisclaimer), ct);
    [HttpGet("lgpd")] public IActionResult Lgpd() => View();

    private async Task<IActionResult> Document(LegalDocumentType type, string fallbackView, CancellationToken ct)
    {
        var published = await documents.PublishedAsync(type, ct);
        ViewBag.CompanyName = configuration["Legal:CompanyName"] ?? configuration["Legal__CompanyName"] ?? "MNSOLUÇÕES TECNOLÓGICAS & CONSULTORIA LTDA";
        ViewBag.TradeName = configuration["Legal:TradeName"] ?? configuration["Legal__TradeName"] ?? "MNSOFT";
        ViewBag.Cnpj = configuration["Legal:Cnpj"] ?? configuration["Legal__Cnpj"] ?? "18.160.057/0001-13";
        ViewBag.SupportEmail = configuration["Legal:SupportEmail"] ?? configuration["Legal__SupportEmail"] ?? "";
        ViewBag.PrivacyContactEmail = configuration["Legal:PrivacyContactEmail"] ?? configuration["Legal__PrivacyContactEmail"] ?? ViewBag.SupportEmail;
        ViewBag.Address = configuration["Legal:Address"] ?? configuration["Legal__Address"];
        if (type == LegalDocumentType.PrivacyNotice && string.IsNullOrWhiteSpace((string?)ViewBag.PrivacyContactEmail))
            logger.LogWarning("Legal:PrivacyContactEmail não configurado; a política orientará o uso do suporte disponível no produto.");
        if (type == LegalDocumentType.PrivacyNotice && string.IsNullOrWhiteSpace((string?)ViewBag.Address))
            logger.LogWarning("Legal:Address não configurado; nenhum endereço será exibido na política de privacidade.");
        return published is null ? View(fallbackView) : View("Document", published);
    }
}
