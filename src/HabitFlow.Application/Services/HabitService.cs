using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed class HabitService(IHabitRepository habits, IHabitCompletionRepository completions, HabitPolicy policy, AuditService audit, ILogger<HabitService> logger)
{
    public async Task<Result<Habit>> CreateAsync(User user, string name, string color, string? category, CancellationToken ct = default)
    {
        try
        {
            var active = await habits.CountActiveByUserAsync(user.Id, ct);
            var can = policy.CanCreate(user, active);
            if (can.IsFailure) return Result<Habit>.Failure(can.Error.Code, can.Error.Message);
            var now = DateTime.UtcNow;
            var habit = new Habit(Guid.NewGuid(), user.Id, name.Trim(), color, category, false, null, now, now);
            await habits.CreateAsync(habit, ct);
            await audit.LogAsync("habit_created", "Hábito criado", AuditSeverity.Info, user.Id, user.Email, new { name }, ct);
            return Result<Habit>.Success(habit);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao criar hábito para {UserId}", user.Id);
            return Result<Habit>.Failure("habit.create_error", "Não foi possível criar o hábito agora.");
        }
    }

    public async Task<IReadOnlyList<Habit>> ListAsync(Guid userId, CancellationToken ct = default)
    {
        try { return await habits.ListByUserAsync(userId, ct); }
        catch (Exception ex) { logger.LogError(ex, "Erro ao listar hábitos de {UserId}", userId); return Array.Empty<Habit>(); }
    }

    public async Task<Result> MarkTodayAsync(User user, Guid habitId, CancellationToken ct = default)
    {
        try { await completions.AddAsync(new HabitCompletion(Guid.NewGuid(), habitId, user.Id, DateOnly.FromDateTime(DateTime.UtcNow), DateTime.UtcNow), ct); return Result.Success(); }
        catch (Exception ex) { logger.LogError(ex, "Erro ao marcar hábito {HabitId}", habitId); return Result.Failure("habit.mark_error", "Não foi possível marcar o hábito."); }
    }

    public async Task<Result> UnmarkTodayAsync(Guid habitId, CancellationToken ct = default)
    {
        try { await completions.DeleteAsync(habitId, DateOnly.FromDateTime(DateTime.UtcNow), ct); return Result.Success(); }
        catch (Exception ex) { logger.LogError(ex, "Erro ao desmarcar hábito {HabitId}", habitId); return Result.Failure("habit.unmark_error", "Não foi possível desmarcar o hábito."); }
    }
}
