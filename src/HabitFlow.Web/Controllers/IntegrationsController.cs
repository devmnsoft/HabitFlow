using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace HabitFlow.Web.Controllers;

[Authorize, Route("integrations")]
public sealed class IntegrationsController(
    CurrentUserContext current,
    IIntegrationRepository repository,
    IntegrationService service,
    ICalendarExportService calendarExport) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (current.ClientId is not { } client) return Forbid();
        ViewBag.ApiKeys = await repository.ListApiKeysAsync(client, current.UserId, ct);
        ViewBag.Calendar = await repository.GetCalendarFeedAsync(client, current.UserId, ct);
        ViewBag.Webhooks = await repository.ListWebhooksAsync(client, current.UserId, ct);
        ViewBag.NewSecret = TempData["IntegrationSecret"];
        ViewBag.CalendarUrl = TempData["CalendarUrl"];
        return View();
    }

    [HttpPost("api-keys")]
    public async Task<IActionResult> CreateKey(string name, string[] scopes, CancellationToken ct)
    {
        if (current.ClientId is not { } client) return Forbid();
        var created = await service.CreateKeyAsync(client, current.UserId, name, scopes, ct);
        TempData["IntegrationSecret"] = created.Secret;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("api-keys/{id:guid}/revoke")]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken ct)
    {
        if (current.ClientId is not { } client) return Forbid();
        if (await repository.RevokeApiKeyAsync(client, current.UserId, id, ct)) await repository.AddAuditAsync(client, current.UserId, "api_key.revoked", new { id }, ct);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("calendar/rotate")]
    public async Task<IActionResult> RotateCalendar(bool enabled, bool includeHabits, CancellationToken ct)
    {
        if (current.ClientId is not { } client) return Forbid();
        var created = await service.RotateCalendarAsync(client, current.UserId, enabled, includeHabits, routines: false, ct);
        TempData["CalendarUrl"] = Url.Action("Feed", "CalendarFeed", new { token=created.Secret }, Request.Scheme);
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
}
