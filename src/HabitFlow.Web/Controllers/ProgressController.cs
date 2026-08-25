using System.Globalization;
using HabitFlow.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize]
[Route("progress")]
public sealed class ProgressController(ProgressCalendarService calendar, UserTimeZoneService timeZones,
    CurrentUserContext currentUser, ILogger<ProgressController> logger) : Controller
{
    [HttpGet("calendar", Name = "ProgressCalendar")]
    public async Task<IActionResult> Calendar(int? year, int? month, CancellationToken ct)
    {
        if (currentUser.ClientId is not Guid clientId) return Forbid();
        var today = timeZones.Today(); var selectedYear = year ?? today.Year; var selectedMonth = month ?? today.Month;
        if (!Valid(selectedYear, selectedMonth)) return BadRequest("O período informado é inválido.");
        try { return View(await calendar.BuildMonthAsync(clientId, currentUser.UserId, selectedYear, selectedMonth, ct)); }
        catch (ProgressPeriodAccessException ex) { Response.StatusCode = StatusCodes.Status403Forbidden; ViewData["AvailableFrom"] = ex.AvailableFrom; return View("CalendarError"); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao carregar calendário de progresso. CorrelationId {CorrelationId}", HttpContext.TraceIdentifier);
            Response.StatusCode = StatusCodes.Status500InternalServerError;
            return View("CalendarError");
        }
    }

    [HttpGet("calendar/data")]
    public async Task<IActionResult> Data(int? year, int? month, CancellationToken ct)
    {
        if (currentUser.ClientId is not Guid clientId) return Forbid();
        var today = timeZones.Today(); var selectedYear = year ?? today.Year; var selectedMonth = month ?? today.Month;
        if (!Valid(selectedYear, selectedMonth)) return Problem("O período informado é inválido.", statusCode: 400);
        try { return Json(await calendar.BuildMonthAsync(clientId, currentUser.UserId, selectedYear, selectedMonth, ct)); }
        catch (ProgressPeriodAccessException ex) { return Problem(title: "Período indisponível no plano atual", detail: "Seus dados continuam guardados. Selecione um período disponível ou compare os planos.", statusCode: 403, extensions: new Dictionary<string, object?> { ["code"] = ProgressPeriodAccessException.Code, ["availableFrom"] = ex.AvailableFrom.ToString("yyyy-MM-dd") }); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) { logger.LogError(ex, "Falha nos dados de progresso. CorrelationId {CorrelationId}", HttpContext.TraceIdentifier); return Problem("Não foi possível carregar seu progresso agora.", statusCode: 500); }
    }

    [HttpGet("day/{date}")]
    public async Task<IActionResult> Day(string date, CancellationToken ct)
    {
        if (currentUser.ClientId is not Guid clientId) return Forbid();
        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var selected)) return BadRequest("A data deve usar o formato yyyy-MM-dd.");
        if (selected > timeZones.Today().AddMonths(12) || selected < new DateOnly(2000, 1, 1)) return BadRequest("A data informada está fora do período permitido.");
        try { return View(await calendar.BuildDayAsync(clientId, currentUser.UserId, selected, ct)); }
        catch (ProgressPeriodAccessException ex) { Response.StatusCode = StatusCodes.Status403Forbidden; ViewData["AvailableFrom"] = ex.AvailableFrom; return View("CalendarError"); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) { logger.LogError(ex, "Falha no detalhe de progresso. CorrelationId {CorrelationId}", HttpContext.TraceIdentifier); return Problem("Não foi possível carregar o detalhe agora.", statusCode: 500); }
    }
    private static bool Valid(int year, int month) => year is >= 2000 and <= 2100 && month is >= 1 and <= 12;
}
