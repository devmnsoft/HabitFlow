using HabitFlow.Domain;
using HabitFlow.Shared;

namespace HabitFlow.Application;

public enum DailyRoutineItemStatus { Upcoming, Available, Completed, Excused, Moved, Missed }
public sealed record DailyRoutineQuery(Guid ClientId, Guid UserId, DateOnly LocalDate);
public sealed record DailyRoutineItem(Guid HabitId, string Name, string Color, string? Category, string? IconCode, Guid? ObjectiveId, TimeOnly? PreferredTime, int EstimatedMinutes, HabitFrequencyType Frequency, DailyRoutineItemStatus Status, int SortOrder, int Version, string NextAction, decimal? TargetQuantity = null, string? TargetUnit = null, string? MinimumVersionName = null, decimal? MinimumVersionQuantity = null);
public sealed record DailyRoutinePlan(DateOnly LocalDate, IReadOnlyList<DailyRoutineItem> Items, int Scheduled, int Completed, int Percentage)
{
    public int Pending => Scheduled - Completed;
}
public sealed record DailyAvailabilityInput(int? AvailableMinutes, TimeOnly? WindowStart, TimeOnly? WindowEnd, IReadOnlySet<Guid> PriorityHabitIds, bool PreferMinimumVersion, IReadOnlySet<Guid> KeepHabitIds);
public sealed record DailyRoutineProposalItem(Guid HabitId, string Name, int? EstimatedMinutes, bool Included, bool UsesMinimumVersion, string Reason);
public sealed record DailyRoutineProposal(DateOnly LocalDate, int? AvailableMinutes, int KnownEstimatedMinutes, bool ExceedsAvailability, IReadOnlyList<DailyRoutineProposalItem> Items, IReadOnlyList<string> Explanations, IReadOnlyList<string> MissingData, IReadOnlyList<string> Alternatives);

public sealed class DailyRoutinePlannerService(IHabitRepository habits, IHabitWeekDayRepository weekDays, IHabitCompletionRepository completions, IHabitScheduleExceptionRepository exceptions, IDailyRoutineOverrideRepository overrides, EffectiveHabitScheduleService schedule, TimeProvider clock, UserTimeZoneService timeZone)
{
    public async Task<DailyRoutinePlan> BuildAsync(DailyRoutineQuery query, CancellationToken ct = default)
    {
        if (query.ClientId == Guid.Empty || query.UserId == Guid.Empty) throw new ArgumentException("Conta e pessoa são obrigatórias.");
        var source = await habits.ListActiveAsync(query.ClientId, query.UserId, ct);
        var days = await weekDays.ListByHabitsAsync(source.Select(x => x.Id), ct);
        var completionSet = (await completions.ListAsync(query.ClientId, query.UserId, query.LocalDate, query.LocalDate, ct)).Select(x => x.HabitId).ToHashSet();
        var exceptionList = await exceptions.ListAsync(query.ClientId, query.UserId, query.LocalDate, query.LocalDate, ct);
        var overrideMap = (await overrides.ListAsync(query.ClientId, query.UserId, query.LocalDate, ct)).ToDictionary(x => x.HabitId);
        var rows = source.Select(ToProgressRow).ToList();
        var dayMap = days.ToDictionary(x => x.Key, x => (IReadOnlySet<int>)x.Value.Select(d => d.DayOfWeek).ToHashSet());
        var effective = await schedule.BuildAsync(new(query.ClientId, query.UserId, query.LocalDate, query.LocalDate, rows, dayMap, exceptionList, overrideMap.Values.ToList(), timeZone.Resolve()));
        var now = TimeOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), timeZone.Resolve()).DateTime);
        var byHabit = source.ToDictionary(x => x.Id);
        var items = effective.Occurrences.Where(x => byHabit.ContainsKey(x.Habit.Id)).Select(occurrence =>
        {
            var h = byHabit[occurrence.Habit.Id];
            overrideMap.TryGetValue(h.Id, out var custom);
            var preferred = occurrence.EffectiveTime;
            var status = occurrence.Status switch
            {
                EffectiveOccurrenceStatus.Excused => DailyRoutineItemStatus.Excused,
                EffectiveOccurrenceStatus.MovedOut => DailyRoutineItemStatus.Moved,
                _ when completionSet.Contains(h.Id) => DailyRoutineItemStatus.Completed,
                _ when preferred.HasValue && preferred > now => DailyRoutineItemStatus.Upcoming,
                _ => DailyRoutineItemStatus.Available
            };
            var action = status switch { DailyRoutineItemStatus.Completed => "Desfazer", DailyRoutineItemStatus.Excused or DailyRoutineItemStatus.Moved => "Restaurar", _ => "Concluir" };
            return new DailyRoutineItem(h.Id,h.Name,h.Color,h.Category,h.IconCode,h.ObjectiveId,preferred,h.EstimatedTimeMinutes ?? 0,h.FrequencyType,status,custom?.SortOrder ?? h.SortOrder,occurrence.ExceptionVersion > 0 ? occurrence.ExceptionVersion : custom?.Version ?? 0,action,h.TargetQuantity,h.TargetUnit,h.MinimumVersionName,h.MinimumVersionQuantity);
        }).OrderBy(x => x.Status == DailyRoutineItemStatus.Completed ? 1 : 0)
          .ThenBy(x => x.PreferredTime.HasValue ? 0 : 1).ThenBy(x => x.PreferredTime)
          .ThenBy(x => x.ObjectiveId.HasValue ? 0 : 1).ThenBy(x => x.EstimatedMinutes)
          .ThenBy(x => x.SortOrder).ThenBy(x => x.Name).ToList();
        var done = items.Count(x => x.Status == DailyRoutineItemStatus.Completed);
        var scheduled = effective.EffectiveOccurrences.Count;
        return new(query.LocalDate,items,scheduled,done,scheduled == 0 ? 0 : (int)Math.Round(done * 100d / scheduled));
    }

    public DailyRoutineProposal Propose(DailyRoutinePlan plan, DailyAvailabilityInput availability)
    {
        var explanations = new List<string>();
        var missing = new List<string>();
        var alternatives = new List<string>();
        var available = availability.AvailableMinutes is > 0 ? availability.AvailableMinutes : null;
        var remaining = available;
        var candidates = plan.Items.Where(x => x.Status is DailyRoutineItemStatus.Available or DailyRoutineItemStatus.Upcoming)
            .OrderByDescending(x => availability.KeepHabitIds.Contains(x.HabitId))
            .ThenByDescending(x => availability.PriorityHabitIds.Contains(x.HabitId))
            .ThenBy(x => x.PreferredTime.HasValue ? 0 : 1)
            .ThenBy(x => x.PreferredTime)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToList();
        if (available is null) missing.Add("Tempo disponível não informado; a proposta preserva a ordem do dia sem limitar duração.");
        if (availability.WindowStart.HasValue != availability.WindowEnd.HasValue) missing.Add("Janela de horário incompleta; informe início e fim para ordenar por bloco.");
        if (availability.WindowStart.HasValue && availability.WindowEnd.HasValue && availability.WindowEnd <= availability.WindowStart) missing.Add("Janela de horário inválida; a proposta ignorou esse intervalo.");

        var items = new List<DailyRoutineProposalItem>();
        foreach (var item in candidates)
        {
            var estimated = item.EstimatedMinutes > 0 ? item.EstimatedMinutes : (int?)null;
            if (estimated is null) missing.Add($"{item.Name}: duração estimada ausente.");
            var forced = availability.KeepHabitIds.Contains(item.HabitId);
            var fits = remaining is null || estimated is null || estimated.Value <= remaining.Value;
            var include = forced || fits;
            if (include && remaining.HasValue && estimated.HasValue) remaining -= estimated.Value;
            var usesMinimum = availability.PreferMinimumVersion && !string.IsNullOrWhiteSpace(item.MinimumVersionName);
            var reason = include
                ? forced ? "Mantido por escolha do usuário." : availability.PriorityHabitIds.Contains(item.HabitId) ? "Incluído por prioridade declarada." : "Incluído por caber na disponibilidade informada."
                : "Ficou fora da sugestão principal por falta de tempo informado; o hábito continua previsto.";
            if (!include) alternatives.Add($"{item.Name}: fazer a versão mínima, mover para outro horário ou manter para depois.");
            items.Add(new(item.HabitId, item.Name, estimated, include, usesMinimum, reason));
        }

        var known = items.Where(x => x.Included && x.EstimatedMinutes.HasValue).Sum(x => x.EstimatedMinutes!.Value);
        if (items.Any(x => x.Included && x.EstimatedMinutes is null)) explanations.Add("Há itens incluídos sem duração conhecida; nenhuma duração foi inventada.");
        if (available.HasValue && known > available.Value) explanations.Add("A proposta excede o tempo disponível por itens mantidos explicitamente.");
        if (availability.PreferMinimumVersion) explanations.Add("Quando disponível, a versão mínima foi sugerida como execução viável, sem contar como alvo completo.");
        if (items.Count == 0) explanations.Add("Nenhum hábito autorizado pendente foi encontrado para hoje.");

        return new(plan.LocalDate, available, known, available.HasValue && known > available.Value, items, explanations, missing.Distinct().ToList(), alternatives);
    }

    private static ProgressHabitRow ToProgressRow(Habit h) => new()
    {
        Id = h.Id, Name = h.Name, Category = h.Category, IsArchived = h.IsArchived, ArchivedAt = h.ArchivedAt,
        CreatedAt = h.StartDate?.ToDateTime(TimeOnly.MinValue) ?? h.CreatedAt, StartDate = h.StartDate, EndDate = h.EndDate,
        FrequencyTypeCode = h.FrequencyType.ToString(), ReminderTime = h.ReminderTime, IsPaused = h.IsPaused, PausedAt = h.PausedAt,
        TargetQuantity = h.TargetQuantity, TargetUnit = h.TargetUnit, MinimumVersionName = h.MinimumVersionName,
        MinimumVersionQuantity = h.MinimumVersionQuantity, RetroactiveAdjustmentDays = h.RetroactiveAdjustmentDays
    };
}

public sealed record RoutineActionResultViewModel(bool Succeeded, string Message, string? ErrorCode = null);

public sealed class DailyRoutineActionService(
    IHabitRepository habits,
    IDailyRoutineOverrideRepository overrides,
    HabitScheduleExceptionService scheduleExceptions,
    TimeProvider clock)
{
    public async Task<RoutineActionResultViewModel> ChangeTimeAsync(Guid clientId, Guid userId, Guid habitId, DateOnly date, TimeOnly preferredTime, int expectedVersion, CancellationToken ct = default)
    {
        var habit = await habits.GetAsync(clientId, userId, habitId, ct);
        if (habit is null) return new(false, "Hábito não encontrado.", "habit.not_found");
        var current = (await overrides.ListAsync(clientId, userId, date, ct)).SingleOrDefault(x => x.HabitId == habitId);
        if ((current?.Version ?? 0) != expectedVersion) return new(false, "A rotina mudou em outra sessão. Atualize a página e tente novamente.", "routine.conflict");
        var now = clock.GetUtcNow();
        await overrides.UpsertAsync(new(current?.Id ?? Guid.NewGuid(), clientId, userId, habitId, date, preferredTime, current?.SortOrder ?? habit.SortOrder, current?.Version ?? 0, current?.CreatedAt ?? now, now), expectedVersion, ct);
        return new(true, $"Horário de hoje alterado para {preferredTime:HH\\:mm}.");
    }

    public async Task<RoutineActionResultViewModel> ReorderAsync(Guid clientId, Guid userId, Guid habitId, DateOnly date, int sortOrder, int expectedVersion, CancellationToken ct = default)
    {
        if (sortOrder is < 0 or > 1000) return new(false, "A posição informada é inválida.", "routine.invalid_order");
        var habit = await habits.GetAsync(clientId, userId, habitId, ct);
        if (habit is null) return new(false, "Hábito não encontrado.", "habit.not_found");
        var current = (await overrides.ListAsync(clientId, userId, date, ct)).SingleOrDefault(x => x.HabitId == habitId);
        if ((current?.Version ?? 0) != expectedVersion) return new(false, "A rotina mudou em outra sessão. Atualize a página e tente novamente.", "routine.conflict");
        var now = clock.GetUtcNow();
        await overrides.UpsertAsync(new(current?.Id ?? Guid.NewGuid(), clientId, userId, habitId, date, current?.PreferredTime ?? habit.ReminderTime, sortOrder, current?.Version ?? 0, current?.CreatedAt ?? now, now), expectedVersion, ct);
        return new(true, "Ordem de hoje atualizada.");
    }

    public async Task<RoutineActionResultViewModel> RestoreAsync(Guid clientId, Guid userId, Guid habitId, DateOnly date, int exceptionVersion, int overrideVersion, CancellationToken ct = default)
    {
        if (await habits.GetAsync(clientId, userId, habitId, ct) is null) return new(false, "Hábito não encontrado.", "habit.not_found");
        var exception = await scheduleExceptions.RestoreOriginalScheduleAsync(clientId, userId, habitId, date, exceptionVersion, ct);
        var custom = (await overrides.ListAsync(clientId, userId, date, ct)).SingleOrDefault(x => x.HabitId == habitId);
        var overrideRestored = custom is null || await overrides.DeleteAsync(clientId, userId, habitId, date, overrideVersion, ct);
        if (!exception.IsSuccess && custom is null) return new(false, exception.Error.Message, exception.Error.Code);
        return overrideRestored ? new(true, "A agenda original foi restaurada.") : new(false, "A rotina mudou em outra sessão. Atualize a página.", "routine.conflict");
    }
}

public sealed class HabitScheduleExceptionService(IHabitRepository habits, IHabitScheduleExceptionRepository exceptions, TimeProvider clock)
{
    public async Task<Result> SetAsync(Guid clientId, Guid userId, Guid habitId, DateOnly date, HabitScheduleExceptionType type, DateOnly? destination, string? reason, CancellationToken ct=default)
    {
        if (await habits.GetAsync(clientId,userId,habitId,ct) is null) return Result.Failure("habit.not_found","Hábito não encontrado.");
        if (type == HabitScheduleExceptionType.Moved && (!destination.HasValue || destination <= date)) return Result.Failure("schedule.invalid_destination","Escolha uma data futura.");
        var now=clock.GetUtcNow();
        var mutation = await exceptions.UpsertAsync(new(Guid.NewGuid(),clientId,userId,habitId,date,type,destination,reason?.Trim(),1,now,now),0,ct);
        return mutation.Succeeded ? Result.Success() : Result.Failure("schedule.conflict","A rotina foi alterada em outra sessão. Atualize a página e tente novamente.");
    }

    public async Task<Result> RestoreOriginalScheduleAsync(Guid clientId, Guid userId, Guid habitId, DateOnly date, int expectedVersion, CancellationToken ct=default)
    {
        if (await habits.GetAsync(clientId,userId,habitId,ct) is null) return Result.Failure("habit.not_found","Hábito não encontrado.");
        var mutation = await exceptions.DeleteAsync(clientId,userId,habitId,date,expectedVersion,ct);
        return mutation.Succeeded ? Result.Success() : Result.Failure("schedule.conflict","A rotina foi alterada em outra sessão. Atualize a página e tente novamente.");
    }
}
