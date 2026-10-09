using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

public sealed record OfflineSyncBatchRequest(IReadOnlyList<OfflineActionRequest> Actions);

public sealed class OfflineController(
    OfflineSyncService syncService,
    IOfflineSyncRepository syncRepo,
    CurrentUserContext current) : Controller
{
    [AllowAnonymous]
    [HttpGet("offline")]
    public IActionResult Index() => View();

    [Authorize]
    [ValidateAntiForgeryToken]
    [HttpPost("offline/sync")]
    public async Task<IActionResult> Sync([FromBody] OfflineSyncBatchRequest request, CancellationToken ct)
    {
        if (current.ClientId is not { } clientId || current.UserId == Guid.Empty)
            return Unauthorized(new { error = "Autenticação requerida para sincronização." });

        if (request?.Actions is null || request.Actions.Count == 0)
            return Ok(new OfflineBatchSyncResponse(0, 0, 0, 0, []));

        var response = await syncService.ProcessSyncBatchAsync(clientId, current.UserId, request.Actions, ct);
        return Ok(response);
    }

    [Authorize]
    [HttpGet("offline/status")]
    public async Task<IActionResult> Status(CancellationToken ct)
    {
        if (current.ClientId is not { } clientId || current.UserId == Guid.Empty)
            return Unauthorized();

        var pending = await syncRepo.CountPendingAsync(clientId, current.UserId, ct);
        var recent = await syncRepo.ListByUserAsync(clientId, current.UserId, null, 10, ct);

        return Json(new
        {
            pendingCount = pending,
            recentItems = recent.Select(r => new
            {
                r.Id,
                r.ActionType,
                r.Status,
                r.ErrorMessage,
                r.ClientCreatedAt,
                r.SyncedAt
            })
        });
    }
}
