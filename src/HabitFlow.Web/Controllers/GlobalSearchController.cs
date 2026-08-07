using HabitFlow.Application;
using HabitFlow.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize]
[Route("global-search")]
public sealed class GlobalSearchController(GlobalSearchService search) : Controller
{
    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Index([FromQuery] string? q, CancellationToken ct)
    {
        var clientId = this.CurrentClientId();
        var userId = this.CurrentUserId();
        if (clientId == Guid.Empty || userId == Guid.Empty) return Forbid();

        var normalized = q?.Trim() ?? string.Empty;
        if (normalized.Length > 80) return BadRequest(new { message = "Use até 80 caracteres na busca." });

        var results = await search.SearchAsync(clientId, userId, normalized, ct: ct);
        var model = new GlobalSearchViewModel(normalized,
            results.Select(item => new GlobalSearchItemViewModel(item.Type, item.Title, item.Description, item.Url, item.Icon)).ToList());
        return Ok(model);
    }
}
