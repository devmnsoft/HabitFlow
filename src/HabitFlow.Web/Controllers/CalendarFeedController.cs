using System.Text;
using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

public sealed class CalendarFeedController(
    IIntegrationRepository integrations,
    ICalendarExportService calendarExport) : ControllerBase
{
    [HttpGet("calendar/{token}.ics")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Feed(string token, DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        if (token.Length != 64 || !token.All(Uri.IsHexDigit)) return NotFound();
        var periodStart = from ?? DateOnly.FromDateTime(DateTime.UtcNow);
        if (periodStart == DateOnly.MaxValue)
            return BadRequest(new ProblemDetails
            {
                Title = "Período inválido",
                Detail = "Informe um período de até 366 dias, com a data inicial antes da data final."
            });
        var periodEnd = to ?? periodStart.AddDays(30);
        if (!CalendarExportService.IsValidPeriod(periodStart, periodEnd))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Período inválido",
                Detail = "Informe um período de até 366 dias, com a data inicial antes da data final."
            });
        }

        var feed = await integrations.FindCalendarFeedAsync(IntegrationService.HashSecret(token), ct);
        if (feed is null) return NotFound();

        var calendar = await calendarExport.ExportAsync(feed, periodStart, periodEnd, ct);
        await integrations.TouchCalendarFeedAsync(feed.Id, ct);
        Response.Headers.CacheControl = "no-store";
        return File(Encoding.UTF8.GetBytes(calendar), "text/calendar; charset=utf-8",
            $"habitflow-{periodStart:yyyyMMdd}-{periodEnd:yyyyMMdd}.ics");
    }
}
