using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HabitFlow.Web.Controllers;

public sealed record CreateHabitApiRequest(string Name, string? Category, string? Frequency, int? TargetDaysPerWeek, string? Description);
public sealed record CreateCheckinApiRequest(Guid HabitId, DateOnly? Date);
public sealed record CreateWebhookApiRequest(string Name, string Url, string[] Events);

[ApiController, Authorize, EnableRateLimiting("public-api")]
[Route("api/v1")]
public sealed class PublicApiController(
    CurrentUserContext current,
    IHabitRepository habits,
    IHabitCompletionRepository completions,
    IHabitTemplateRepository templates,
    INotificationRepository notifications,
    IIntegrationRepository integrations,
    WebhookDispatcherService webhookDispatcher,
    PlanEntitlementService entitlements) : ControllerBase
{
    private bool HasScope(string scope)
    {
        if (User.Identity?.AuthenticationType?.Contains("Cookies", StringComparison.OrdinalIgnoreCase) == true)
            return true;

        var dotScope = scope.Replace(':', '.');
        var colonScope = scope.Replace('.', ':');
        return User.HasClaim("scope", dotScope) || User.HasClaim("scope", colonScope);
    }

    private async Task<IActionResult?> ValidateTenantAndPlanAsync(CancellationToken ct)
    {
        if (current.ClientId is not { } clientId || current.UserId == Guid.Empty)
            return Unauthorized(new { error = new { code = "tenant_required", message = "Tenant obrigatório." } });

        var allowed = await entitlements.CanUseFeatureAsync(current.UserId, PlanFeatureCodes.PublicApi, ct);
        if (!allowed && !User.Identity!.AuthenticationType!.Contains("Cookies", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                error = new { code = "plan_feature_restricted", message = "Acesso à API pública restrito para o plano contratado." }
            });
        }

        return null;
    }

    [HttpGet("habits")]
    public async Task<IActionResult> GetHabits([FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        if (!User.HasClaim("scope", "habits.read") && !HasScope("habits:read")) return Forbid();
        if (await ValidateTenantAndPlanAsync(ct) is { } errorResult) return errorResult;

        var clientId = current.ClientId!.Value;
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var rows = await habits.ListAsync(clientId, current.UserId, ct);
        var data = rows.Skip((page - 1) * pageSize).Take(pageSize).Select(h => new
        {
            h.Id,
            h.Name,
            h.Category,
            frequency = h.FrequencyType.ToString(),
            targetDaysPerWeek = h.TargetPerWeek ?? 7,
            h.IsPaused,
            h.CreatedAt
        });

        return Ok(new { data, meta = new { page, pageSize, total = rows.Count } });
    }

    [HttpPost("habits")]
    public async Task<IActionResult> CreateHabit([FromBody] CreateHabitApiRequest request, CancellationToken ct = default)
    {
        if (!HasScope("habits:write")) return Forbid();
        if (await ValidateTenantAndPlanAsync(ct) is { } errorResult) return errorResult;

        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length < 2)
            return BadRequest(new { error = new { code = "invalid_name", message = "Nome do hábito deve ter no mínimo 2 caracteres." } });

        var existing = await habits.ListAsync(current.ClientId!.Value, current.UserId, ct);
        var canCreate = await entitlements.CanCreateHabitAsync(current.UserId, existing.Count + 1, ct);
        if (!canCreate)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                error = new { code = "plan_limit_reached", message = "Limite de hábitos ativos do plano atingido." }
            });
        }

        var freq = Enum.TryParse<HabitFrequencyType>(request.Frequency, true, out var f) ? f : HabitFrequencyType.Daily;
        var targetDays = Math.Clamp(request.TargetDaysPerWeek ?? 7, 1, 7);

        var habit = new Habit(
            Guid.NewGuid(),
            current.UserId,
            request.Name.Trim(),
            "#10B981",
            request.Category?.Trim() ?? "Geral",
            false,
            null,
            DateTime.UtcNow,
            DateTime.UtcNow,
            freq,
            targetDays,
            null,
            request.Description?.Trim(),
            0,
            current.ClientId!.Value
        );

        await habits.CreateAsync(habit, ct);

        // Despacha webhook se configurado
        await webhookDispatcher.DispatchEventAsync(current.ClientId!.Value, current.UserId, WebhookEvents.HabitCreated, new
        {
            habitId = habit.Id,
            name = habit.Name,
            category = habit.Category
        }, ct);

        return Created($"/api/v1/habits/{habit.Id}", new { data = habit });
    }

    [HttpPost("checkins")]
    public async Task<IActionResult> CreateCheckin([FromBody] CreateCheckinApiRequest request, CancellationToken ct = default)
    {
        if (!HasScope("checkins:write")) return Forbid();
        if (await ValidateTenantAndPlanAsync(ct) is { } errorResult) return errorResult;

        var habit = await habits.GetAsync(current.ClientId!.Value, current.UserId, request.HabitId, ct);
        if (habit is null || habit.IsArchived)
            return NotFound(new { error = new { code = "habit_not_found", message = "Hábito não encontrado." } });

        var date = request.Date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var mutation = await completions.AddIfMissingAsync(current.ClientId!.Value, current.UserId, habit.Id, date, Guid.NewGuid(), ct);
        if (mutation.Created)
        {
            await webhookDispatcher.DispatchEventAsync(current.ClientId!.Value, current.UserId, WebhookEvents.CheckinCreated, new
            {
                habitId = habit.Id,
                completionId = mutation.CompletionId,
                date = date.ToString("yyyy-MM-dd")
            }, ct);

            await webhookDispatcher.DispatchEventAsync(current.ClientId!.Value, current.UserId, WebhookEvents.HabitCompleted, new
            {
                habitId = habit.Id,
                date = date.ToString("yyyy-MM-dd")
            }, ct);
        }

        return Ok(new { data = new { habitId = habit.Id, date = date.ToString("yyyy-MM-dd"), completed = true } });
    }

    [HttpGet("reports")]
    public async Task<IActionResult> GetReports(CancellationToken ct = default)
    {
        if (!HasScope("reports:read")) return Forbid();
        if (await ValidateTenantAndPlanAsync(ct) is { } errorResult) return errorResult;

        var userHabits = await habits.ListAsync(current.ClientId!.Value, current.UserId, ct);
        var totalScheduled = userHabits.Count * 7;
        var completedList = await completions.ListByUserAsync(current.UserId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)), ct);
        var completedCount = completedList.Count;

        var rate = totalScheduled > 0 ? (decimal)completedCount / totalScheduled * 100 : 0m;

        return Ok(new
        {
            data = new
            {
                activeHabits = userHabits.Count,
                weeklyScheduled = totalScheduled,
                weeklyCompleted = completedCount,
                completionRate = Math.Round(rate, 1)
            }
        });
    }

    [HttpGet("templates")]
    public async Task<IActionResult> GetTemplates([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        if (!HasScope("templates:read")) return Forbid();
        if (await ValidateTenantAndPlanAsync(ct) is { } errorResult) return errorResult;

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var all = await templates.ListActiveAsync(current.ClientId, ct);
        var data = all.Skip((page - 1) * pageSize).Take(pageSize).Select(t => new
        {
            t.Id,
            t.Name,
            t.Category,
            frequencyType = t.SuggestedFrequency,
            targetPerWeek = t.SuggestedTargetPerWeek,
            t.Description
        });

        return Ok(new { data, meta = new { page, pageSize, total = all.Count } });
    }

    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotifications([FromQuery] int limit = 20, CancellationToken ct = default)
    {
        if (!HasScope("notifications:read")) return Forbid();
        if (await ValidateTenantAndPlanAsync(ct) is { } errorResult) return errorResult;

        limit = Math.Clamp(limit, 1, 50);
        var notifPage = await notifications.SearchAsync(new NotificationQuery(current.ClientId!.Value, current.UserId, Filter: "all", Page: 1, PageSize: limit), ct);
        var data = notifPage.Items.Select(n => new
        {
            n.Id,
            n.Type,
            n.Title,
            n.Message,
            n.IsRead,
            n.CreatedAt
        });

        return Ok(new { data });
    }

    [HttpGet("webhooks")]
    public async Task<IActionResult> GetWebhooks(CancellationToken ct = default)
    {
        if (!HasScope("webhooks:manage")) return Forbid();
        if (await ValidateTenantAndPlanAsync(ct) is { } errorResult) return errorResult;

        var hooks = await integrations.ListWebhooksAsync(current.ClientId!.Value, current.UserId, ct);
        var data = hooks.Select(h => new
        {
            h.Id,
            h.Name,
            h.Url,
            h.Events,
            h.Enabled,
            h.IsPaused,
            h.ConsecutiveFailures,
            h.CreatedAt,
            h.LastSuccessAt
        });

        return Ok(new { data });
    }

    [HttpPost("webhooks")]
    public async Task<IActionResult> CreateWebhook([FromBody] CreateWebhookApiRequest request, CancellationToken ct = default)
    {
        if (!HasScope("webhooks:manage")) return Forbid();
        if (await ValidateTenantAndPlanAsync(ct) is { } errorResult) return errorResult;

        try
        {
            var created = await webhookDispatcher.CreateWebhookAsync(
                current.ClientId!.Value,
                current.UserId,
                request.Name,
                request.Url,
                request.Events,
                ct
            );

            return Created($"/api/v1/webhooks/{created.Value.Id}", new
            {
                data = new
                {
                    created.Value.Id,
                    created.Value.Name,
                    created.Value.Url,
                    created.Value.Events,
                    secret = created.Secret // Exibido apenas nesta resposta
                }
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = new { code = "invalid_webhook", message = ex.Message } });
        }
    }

    [HttpGet("profile")]
    public IActionResult Profile()
    {
        if (!HasScope("profile:read")) return Forbid();
        return Ok(new { data = new { id = current.UserId, current.Name, current.Email, clientId = current.ClientId } });
    }
}
