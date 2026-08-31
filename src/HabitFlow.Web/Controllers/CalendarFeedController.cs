using System.Text;
using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

public sealed class CalendarFeedController(IIntegrationRepository integrations, IHabitRepository habits) : ControllerBase
{
    [HttpGet("calendar/{token}.ics")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Feed(string token, CancellationToken ct)
    {
        if (token.Length != 64) return NotFound();
        var feed = await integrations.FindCalendarFeedAsync(IntegrationService.HashSecret(token), ct);
        if (feed is null) return NotFound();
        var output = new StringBuilder("BEGIN:VCALENDAR\r\nVERSION:2.0\r\nPRODID:-//HabitFlow//Calendar 6.19.1//PT-BR\r\nCALSCALE:GREGORIAN\r\n");
        if (feed.IncludeHabits)
            foreach (var habit in await habits.ListActiveAsync(feed.ClientId, feed.UserId, ct))
                output.Append("BEGIN:VEVENT\r\nUID:").Append(habit.Id).Append("@habitflow\r\nDTSTART;VALUE=DATE:").Append(DateTime.UtcNow.ToString("yyyyMMdd")).Append("\r\nRRULE:FREQ=DAILY\r\nSUMMARY:").Append(Escape(habit.Name)).Append("\r\nEND:VEVENT\r\n");
        output.Append("END:VCALENDAR\r\n");
        await integrations.TouchCalendarFeedAsync(feed.Id, ct);
        Response.Headers.CacheControl = "no-store";
        return Content(output.ToString(), "text/calendar; charset=utf-8", Encoding.UTF8);
    }
    static string Escape(string value) => value.Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,").Replace("\r", "").Replace("\n", "\\n");
}
