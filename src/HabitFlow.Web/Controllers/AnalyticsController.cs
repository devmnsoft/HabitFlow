using System.Diagnostics;
using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize]
public sealed class AnalyticsController(IAnalyticsQueryService analytics, CurrentTenantService tenant, UserTimeZoneService timeZones,
    ILogger<AnalyticsController> logger) : Controller
{
    private static readonly EventId DashboardViewed = new(618601, "analytics.opened");
    private static readonly EventId InvalidFilter = new(618602, "analytics.invalid_filter");

    [HttpGet("analytics")]
    [HttpGet("my-evolution")]
    public async Task<IActionResult> Index([FromQuery] DateOnly? start, [FromQuery] DateOnly? end, [FromQuery] Guid? habit,
        [FromQuery] string? category, [FromQuery] string? status, [FromQuery] HabitFrequencyType? frequency,
        [FromQuery] bool? reminder, [FromQuery] bool? completed, CancellationToken ct)
    {
        var timer=Stopwatch.StartNew(); var today=timeZones.Today();
        try
        {
            var period=AnalyticsPeriod.Create(start??today.AddDays(-27),end??today,today);
            var model=await analytics.GetMyEvolutionAsync(tenant.RequireCurrentClientId(),this.CurrentUserId(),new(period,habit,category,status,frequency,reminder,null,completed),ct);
            logger.LogInformation(DashboardViewed,"analytics.opened Code={Code} CorrelationId={CorrelationId} TenantId={TenantId} UserId={UserId} Period={Start}:{End} Status={Status} DurationMs={DurationMs}","ANALYTICS_OPENED",HttpContext.TraceIdentifier,tenant.RequireCurrentClientId(),this.CurrentUserId(),period.Start,period.End,"success",timer.ElapsedMilliseconds);
            return View(model);
        }
        catch(ArgumentException ex)
        {
            logger.LogWarning(InvalidFilter,"analytics.invalid_filter Code={Code} CorrelationId={CorrelationId} ReportType={ReportType} Result={Result} DurationMs={DurationMs}","ANALYTICS_INVALID_FILTER",HttpContext.TraceIdentifier,"my-evolution","invalid",timer.ElapsedMilliseconds);
            ModelState.AddModelError(string.Empty,ex.Message); ViewData["FriendlyError"]=ex.Message; return View("InvalidFilter");
        }
    }
}
