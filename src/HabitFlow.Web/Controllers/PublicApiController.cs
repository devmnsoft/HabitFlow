using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HabitFlow.Web.Controllers;

[ApiController, Authorize, EnableRateLimiting("public-api")]
[Route("api/v1")]
public sealed class PublicApiController(CurrentUserContext current, IHabitRepository habits) : ControllerBase
{
    [HttpGet("habits")]
    public async Task<IActionResult> Habits([FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        if (!User.HasClaim("scope", "habits.read") && !User.Identity!.AuthenticationType!.Contains("Cookies", StringComparison.OrdinalIgnoreCase)) return Forbid();
        if (current.ClientId is not { } clientId || current.UserId == Guid.Empty) return Unauthorized(new { error = new { code="tenant_required", message="Tenant obrigatório." } });
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var rows = await habits.ListAsync(clientId, current.UserId, ct);
        var data = rows.Skip((page - 1) * pageSize).Take(pageSize).Select(h => new { h.Id, h.Name, h.Category, frequency=h.FrequencyType.ToString(), h.IsPaused, h.CreatedAt });
        return Ok(new { data, meta = new { page, pageSize, total = rows.Count } });
    }

    [HttpGet("profile")]
    public IActionResult Profile()
    {
        if (!User.HasClaim("scope", "profile.read")) return Forbid();
        return Ok(new { data = new { id=current.UserId, current.Name, current.Email } });
    }
}
