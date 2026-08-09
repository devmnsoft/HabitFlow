using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed class HabitService(IHabitRepository habits, IHabitCompletionRepository completions, IHabitWeekDayRepository weekDays, HabitScheduleService schedule, HabitPolicy policy, AuditService audit, NotificationService notifications, CompleteHabitUseCase completeHabit, UndoHabitCompletionUseCase undoHabit, UserTimeZoneService timeZone, ILogger<HabitService> logger)
{
    public Task<Result<Habit>> CreateAsync(User user, string name, string color, string? category, CancellationToken ct = default) =>
        CreateAsync(user, name, color, category, HabitFrequencyType.Daily, null, null, null, Array.Empty<int>(), ct);

    public async Task<Result<Habit>> CreateAsync(User user, string name, string color, string? category, HabitFrequencyType frequencyType, int? targetPerWeek, TimeOnly? reminderTime, string? notes, IReadOnlyCollection<int> selectedDays, CancellationToken ct = default)
    {
        try
        {
            var validation = schedule.ValidateFrequency(frequencyType, targetPerWeek, selectedDays);
            if (validation.IsFailure) return Result<Habit>.Failure(validation.Error.Code, validation.Error.Message);
            var active = await habits.CountActiveByUserAsync(user.Id, ct);
            var can = policy.CanCreate(user, active);
            if (can.IsFailure) return Result<Habit>.Failure(can.Error.Code, can.Error.Message);
            var now = DateTime.UtcNow;
            var habit = new Habit(Guid.NewGuid(), user.Id, name.Trim(), color, category, false, null, now, now, frequencyType, targetPerWeek, reminderTime, notes, 0, user.ClientId);
            await habits.CreateAsync(habit, ct);
            if (frequencyType == HabitFrequencyType.CustomWeekly) await weekDays.ReplaceAsync(habit.Id, selectedDays, ct);
            await audit.LogAsync("habit_created", "Hábito criado", AuditSeverity.Info, user.Id, user.Email, new { name, frequencyType }, ct);
            if (active == 0) await notifications.CreateAsync(user.Id, "welcome", "Bem-vindo ao HabitFlow", "Seu primeiro hábito foi criado. Comece pequeno e mantenha consistência.", "habit", habit.Id, ct);
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
        try
        {
            if (!user.ClientId.HasValue) return Result.Failure("client.required", "É necessário selecionar uma conta para acessar hábitos pessoais.");
            var result = await completeHabit.ExecuteAsync(new(user.ClientId.Value, user.Id, habitId, timeZone.Today(), Guid.NewGuid().ToString("N"), "LegacyAdapter", Guid.NewGuid().ToString("N")), ct);
            return result.IsSuccess ? Result.Success() : Result.Failure(result.Error.Code, result.Error.Message);
        }
        catch (Exception ex) { logger.LogError(ex, "Erro ao marcar hábito {HabitId}", habitId); return Result.Failure("habit.mark_error", "Não foi possível marcar o hábito."); }
    }

    public async Task<Result> UnmarkTodayAsync(User user, Guid habitId, CancellationToken ct = default)
    {
        try
        {
            if (!user.ClientId.HasValue) return Result.Failure("client.required", "É necessário selecionar uma conta para acessar hábitos pessoais.");
            var result = await undoHabit.ExecuteAsync(new(user.ClientId.Value, user.Id, habitId, timeZone.Today(), Guid.NewGuid().ToString("N"), "LegacyAdapter", Guid.NewGuid().ToString("N")), ct);
            return result.IsSuccess ? Result.Success() : Result.Failure(result.Error.Code, result.Error.Message);
        }
        catch (Exception ex) { logger.LogError(ex, "Erro ao desmarcar hábito {HabitId}", habitId); return Result.Failure("habit.unmark_error", "Não foi possível desmarcar o hábito."); }
    }
}
