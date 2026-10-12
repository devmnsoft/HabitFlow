using HabitFlow.Application;
using HabitFlow.Domain;
using HabitFlow.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize]
[Route("journeys")]
public sealed class JourneysController(HabitJourneyService journeys, HabitQueryService habitQueries, IUserFacingErrorMapper errors) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] JourneyCatalogQuery query, CancellationToken ct) =>
        View(new JourneysIndexViewModel(await journeys.ListAsync(this.CurrentClientId(), query, ct)));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var details = await journeys.DetailsAsync(id, this.CurrentClientId(), this.CurrentUserId(), ct);
        var existing = await habitQueries.SearchAsync(this.CurrentClientId(), this.CurrentUserId(), new HabitListQuery(PageSize: 200), ct);
        return details is null ? NotFound() : View(new JourneyDetailsViewModel(details, existing.Items));
    }

    [HttpPost("{id:guid}/join"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Join(Guid id, bool confirmed, CancellationToken ct)
    {
        var selections = BuildSelectionsFromForm();
        var result = await journeys.JoinAsync(new JoinHabitJourneyCommand(this.CurrentClientId(), this.CurrentUserId(), id, confirmed, selections, HttpContext.TraceIdentifier), ct);
        if (result.IsFailure) TempData["Warning"] = errors.ToPublicMessage(result.Error.Code, "journeys");
        else TempData["Success"] = "Programa ativado com os habitos confirmados. Seu acompanhamento usa registros reais.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:guid}/leave"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Leave(Guid id, CancellationToken ct)
    {
        await journeys.LeaveAsync(id, this.CurrentClientId(), this.CurrentUserId(), HttpContext.TraceIdentifier, ct);
        TempData["Success"] = "Voce saiu da jornada. Seu historico foi preservado.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private IReadOnlyList<JoinHabitJourneyHabitSelection> BuildSelectionsFromForm()
    {
        var selected = Request.Form["selectedStepIds"]
            .Select(value => Guid.TryParse(value, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        return selected.Select(stepId =>
        {
            var key = stepId.ToString("N");
            var name = Request.Form[$"habitName_{key}"].ToString();
            var frequency = ParseFrequency(Request.Form[$"frequency_{key}"].ToString());
            var reminder = TimeOnly.TryParse(Request.Form[$"reminder_{key}"].ToString(), out var parsedReminder) ? parsedReminder : (TimeOnly?)null;
            var selectedDays = ParseDays(Request.Form[$"days_{key}"]);
            var existingHabitId = Guid.TryParse(Request.Form[$"existingHabitId_{key}"].ToString(), out var parsedHabitId) ? parsedHabitId : (Guid?)null;
            var duplicateExisting = Request.Form[$"duplicateExisting_{key}"].Any(value => value == "true");
            int? target = frequency == HabitFrequencyType.CustomWeekly ? selectedDays.Count : null;
            return new JoinHabitJourneyHabitSelection(stepId, name, frequency, target, selectedDays, reminder, existingHabitId, duplicateExisting);
        }).ToList();
    }

    private static HabitFrequencyType ParseFrequency(string? value) =>
        Enum.TryParse<HabitFrequencyType>(value, true, out var frequency) ? frequency : HabitFrequencyType.Daily;

    private static IReadOnlyCollection<int> ParseDays(IEnumerable<string> values)
    {
        var days = values.Select(value => int.TryParse(value, out var day) ? day : -1)
            .Where(day => day is >= 0 and <= 6)
            .Distinct()
            .OrderBy(day => day)
            .ToArray();
        return days.Length == 0 ? [1, 2, 3, 4, 5] : days;
    }
}
