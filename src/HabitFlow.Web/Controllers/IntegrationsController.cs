using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize, Route("integrations")]
public sealed class IntegrationsController(CurrentUserContext current, IIntegrationRepository repository, IntegrationService service) : Controller
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
    public async Task<IActionResult> RotateCalendar(bool enabled, bool includeHabits, bool includeRoutines, CancellationToken ct)
    {
        if (current.ClientId is not { } client) return Forbid();
        var created = await service.RotateCalendarAsync(client, current.UserId, enabled, includeHabits, includeRoutines, ct);
        TempData["CalendarUrl"] = Url.Action("Feed", "CalendarFeed", new { token=created.Secret }, Request.Scheme);
        return RedirectToAction(nameof(Index));
    }
}
