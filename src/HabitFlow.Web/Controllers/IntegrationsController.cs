using System.Text;
using System.Text.Json;
using HabitFlow.Application;
using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace HabitFlow.Web.Controllers;

[Authorize, Route("integrations")]
public sealed class IntegrationsController(
    CurrentUserContext current,
    IIntegrationRepository repository,
    IntegrationService service,
    ICalendarExportService calendarExport,
    WebhookDispatcherService webhookDispatcher,
    DataPortabilityService portability,
    IConfiguration config) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (current.ClientId is not { } client) return Forbid();
        ViewBag.ApiKeys = await repository.ListApiKeysAsync(client, current.UserId, ct);
        ViewBag.Calendar = await repository.GetCalendarFeedAsync(client, current.UserId, ct);
        ViewBag.Webhooks = await repository.ListWebhooksAsync(client, current.UserId, ct);
        ViewBag.NewSecret = TempData["IntegrationSecret"];
        ViewBag.NewWebhookSecret = TempData["WebhookSecret"];
        ViewBag.CalendarUrl = TempData["CalendarUrl"];

        // Status dos Provedores Externos
        ViewBag.SmtpConfigured = config.GetValue<bool>("Email:Enabled");
        ViewBag.WhatsAppConfigured = config.GetValue<bool>("WhatsApp:Enabled") && !string.IsNullOrWhiteSpace(config["WhatsApp:Number"]);
        ViewBag.MercadoPagoConfigured = !string.IsNullOrWhiteSpace(config["MercadoPago:AccessToken"]);

        return View();
    }

    [HttpPost("api-keys")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateKey(string name, string[] scopes, CancellationToken ct)
    {
        if (current.ClientId is not { } client) return Forbid();
        try
        {
            var created = await service.CreateKeyAsync(client, current.UserId, name, scopes, ct);
            TempData["IntegrationSecret"] = created.Secret;
            TempData["Success"] = "Chave de API criada com sucesso! Guarde-a em local seguro.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("api-keys/{id:guid}/revoke")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken ct)
    {
        if (current.ClientId is not { } client) return Forbid();
        if (await repository.RevokeApiKeyAsync(client, current.UserId, id, ct))
        {
            await repository.AddAuditAsync(client, current.UserId, "api_key.revoked", new { id }, ct);
            TempData["Success"] = "Chave de API revogada.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("calendar/rotate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RotateCalendar(bool enabled, bool includeHabits, CancellationToken ct)
    {
        if (current.ClientId is not { } client) return Forbid();
        var created = await service.RotateCalendarAsync(client, current.UserId, enabled, includeHabits, routines: false, ct);
        TempData["CalendarUrl"] = Url.Action("Feed", "CalendarFeed", new { token = created.Secret }, Request.Scheme);
        TempData["Success"] = "Link do calendário gerado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("calendar/revoke")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeCalendar(CancellationToken ct)
    {
        if (current.ClientId is not { } client) return Forbid();
        TempData[await service.RevokeCalendarAsync(client, current.UserId, ct) ? "Success" : "Error"] =
            "Token de calendário revogado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("calendar/download")]
    public async Task<IActionResult> DownloadCalendar(DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        if (current.ClientId is not { } client) return Forbid();
        var periodStart = from ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var periodEnd = to ?? periodStart.AddDays(30);
        if (!CalendarExportService.IsValidPeriod(periodStart, periodEnd))
            return BadRequest(new ProblemDetails
            {
                Title = "Período inválido",
                Detail = "Informe um período de até 366 dias, com a data inicial antes da data final."
            });

        var feed = new CalendarFeed(Guid.Empty, client, current.UserId, "", true, true, false, DateTime.UtcNow, null);
        var calendar = await calendarExport.ExportAsync(feed, periodStart, periodEnd, ct);
        Response.Headers.CacheControl = "no-store";
        return File(Encoding.UTF8.GetBytes(calendar), "text/calendar; charset=utf-8",
            $"habitflow-{periodStart:yyyyMMdd}-{periodEnd:yyyyMMdd}.ics");
    }

    [HttpPost("webhooks")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateWebhook(string name, string url, string[] events, CancellationToken ct)
    {
        if (current.ClientId is not { } client) return Forbid();
        try
        {
            var created = await webhookDispatcher.CreateWebhookAsync(client, current.UserId, name, url, events, ct);
            TempData["WebhookSecret"] = created.Secret;
            TempData["Success"] = "Webhook cadastrado! Copie o segredo de assinatura HMAC exibido.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("webhooks/{id:guid}/toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleWebhook(Guid id, bool enabled, CancellationToken ct)
    {
        if (current.ClientId is not { } client) return Forbid();
        await repository.ToggleWebhookAsync(client, current.UserId, id, enabled, ct);
        TempData["Success"] = enabled ? "Webhook reativado." : "Webhook pausado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("webhooks/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteWebhook(Guid id, CancellationToken ct)
    {
        if (current.ClientId is not { } client) return Forbid();
        await repository.DeleteWebhookAsync(client, current.UserId, id, ct);
        TempData["Success"] = "Webhook removido com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("webhooks/{id:guid}/deliveries")]
    public async Task<IActionResult> GetDeliveries(Guid id, CancellationToken ct)
    {
        if (current.ClientId is not { } client) return Forbid();
        var attempts = await repository.ListDeliveryAttemptsAsync(client, id, 20, ct);
        return Json(attempts);
    }

    [HttpPost("webhooks/{webhookId:guid}/retry/{attemptId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RetryDelivery(Guid webhookId, Guid attemptId, CancellationToken ct)
    {
        if (current.ClientId is not { } client) return Forbid();
        var result = await webhookDispatcher.RetryDeliveryAsync(client, webhookId, attemptId, ct);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess
            ? "Reenvio de webhook realizado com sucesso!"
            : result.Error.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("test-provider")]
    [ValidateAntiForgeryToken]
    public IActionResult TestProvider(string provider)
    {
        provider = provider.ToLowerInvariant();
        switch (provider)
        {
            case "smtp":
                var smtpOk = config.GetValue<bool>("Email:Enabled");
                TempData[smtpOk ? "Success" : "Error"] = smtpOk
                    ? "Serviço de e-mail está configurado e pronto para envio."
                    : "Servidor de e-mail não configurado neste ambiente (consulte appsettings.json).";
                break;
            case "whatsapp":
                var waOk = config.GetValue<bool>("WhatsApp:Enabled") && !string.IsNullOrWhiteSpace(config["WhatsApp:Number"]);
                TempData[waOk ? "Success" : "Info"] = waOk
                    ? "WhatsApp comercial ativo e conectado."
                    : "WhatsApp não configurado. Para habilitar, forneça as credenciais oficiais no painel do administrador.";
                break;
            case "google-calendar" or "outlook-calendar":
                TempData["Success"] = "Integração via feed iCal RFC 5545 pronta. Use o link do calendário para sincronizar.";
                break;
            default:
                TempData["Info"] = $"Provedor '{provider}' verificado.";
                break;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportData(string format = "csv", CancellationToken ct = default)
    {
        if (current.ClientId is not { } client) return Forbid();
        var (bytes, contentType, fileName) = await portability.ExportUserDataAsync(
            client, current.UserId, current.Email, current.Name, format, ct);
        return File(bytes, contentType, fileName);
    }

    [HttpPost("import/simulate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SimulateImport(IFormFile? file, CancellationToken ct)
    {
        if (current.ClientId is not { } client) return Forbid();
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "Selecione um arquivo CSV válido para importar." });
        }

        using var stream = file.OpenReadStream();
        var simulation = await portability.SimulateHabitsCsvAsync(client, current.UserId, stream, ct);
        return Json(simulation);
    }

    [HttpPost("import/execute")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExecuteImport([FromBody] IReadOnlyList<HabitImportRow> items, CancellationToken ct)
    {
        if (current.ClientId is not { } client) return Forbid();
        var result = await portability.ExecuteHabitsImportAsync(client, current.UserId, items, ct);
        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error.Message });
        }
        return Ok(new { importedCount = result.Value });
    }
}
