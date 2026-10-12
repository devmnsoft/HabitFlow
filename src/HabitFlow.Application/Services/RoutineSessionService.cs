using HabitFlow.Domain;
using HabitFlow.Shared;

namespace HabitFlow.Application;

public sealed record RoutineSessionPrepareItem(Guid HabitId, bool Selected = true);
public sealed record RoutineSessionPrepareCommand(Guid ClientId, Guid UserId, string Name, int? AvailableMinutes, IReadOnlyList<RoutineSessionPrepareItem> Items, string IdempotencyKey, string CorrelationId, string? ActiveAccountSessionId);
public sealed record RoutineSessionItemCommand(Guid ClientId, Guid UserId, Guid SessionId, Guid ItemId, DateOnly LocalDate, decimal? Quantity, string? Unit, int? EffectiveMinutes, string IdempotencyKey, string CorrelationId);
public sealed record RoutineSessionCommand(Guid ClientId, Guid UserId, Guid SessionId, string IdempotencyKey, string CorrelationId);
public sealed record RoutineSessionCloseCommand(Guid ClientId, Guid UserId, Guid SessionId, string? Reflection, string IdempotencyKey, string CorrelationId);
public sealed record RoutineSessionProposalItem(Guid HabitId, string Name, string? Category, int SortOrder, int? EstimatedMinutes, decimal? MinimumQuantity, string? MinimumName, decimal? TargetQuantity, string? TargetUnit, bool FitsAvailableTime);
public sealed record RoutineSessionProposal(string Name, int? AvailableMinutes, int KnownEstimatedMinutes, bool HasUnknownDurations, IReadOnlyList<RoutineSessionProposalItem> Items, string Message);
public sealed record RoutineSessionResult(RoutineSessionDetails Details, string Message);

public sealed class RoutineSessionService(
    IRoutineSessionRepository sessions,
    IHabitRepository habits,
    CompleteHabitUseCase completeHabit,
    AuditService audit,
    TimeProvider clock,
    UserTimeZoneService timeZone)
{
    public async Task<RoutineSessionProposal> ProposeAsync(Guid clientId, Guid userId, int? availableMinutes, IReadOnlyList<Guid>? preferredHabitIds, CancellationToken ct = default)
    {
        var activeHabits = (await habits.ListActiveAsync(clientId, userId, ct)).Where(x => !x.IsPaused).ToList();
        if (preferredHabitIds is { Count: > 0 })
        {
            var preferred = preferredHabitIds.ToHashSet();
            activeHabits = activeHabits.Where(x => preferred.Contains(x.Id)).ToList();
        }

        var ordered = activeHabits
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.EstimatedTimeMinutes ?? int.MaxValue)
            .ThenBy(x => x.CreatedAt)
            .ToList();

        var remaining = availableMinutes;
        var items = new List<RoutineSessionProposalItem>();
        foreach (var habit in ordered)
        {
            var estimated = habit.EstimatedTimeMinutes;
            var fits = remaining is null || estimated is null || estimated <= remaining.Value || items.Count == 0;
            if (fits && remaining is not null && estimated is not null) remaining = Math.Max(0, remaining.Value - estimated.Value);
            items.Add(new(habit.Id, habit.Name, habit.Category, items.Count + 1, estimated,
                habit.MinimumVersionQuantity, habit.MinimumVersionName, habit.TargetQuantity, habit.TargetUnit, fits));
        }

        var known = items.Where(x => x.EstimatedMinutes.HasValue && x.FitsAvailableTime).Sum(x => x.EstimatedMinutes!.Value);
        var unknown = items.Any(x => !x.EstimatedMinutes.HasValue);
        var message = availableMinutes is null
            ? "Proposta preparada sem limite de tempo. Durações desconhecidas permanecem identificadas."
            : unknown
                ? "Proposta preparada com estimativas disponíveis; alguns hábitos ainda não têm duração cadastrada."
                : known <= availableMinutes.Value
                    ? "A proposta cabe no tempo informado."
                    : "A proposta prioriza os primeiros itens e mostra alternativas para continuar depois.";
        return new("Sessão de rotina", availableMinutes, known, unknown, items, message);
    }

    public async Task<Result<RoutineSessionResult>> PrepareAsync(RoutineSessionPrepareCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
            return Result<RoutineSessionResult>.Failure("routine_session.idempotency_required", "Não foi possível confirmar a sessão com segurança.");
        if (await sessions.FindByIdempotencyKeyAsync(command.ClientId, command.UserId, command.IdempotencyKey, ct) is { } existing)
        {
            var existingDetails = await sessions.GetAsync(command.ClientId, command.UserId, existing.Id, ct);
            return existingDetails is null
                ? Result<RoutineSessionResult>.Failure("routine_session.not_found", "Sessão não encontrada.")
                : Result<RoutineSessionResult>.Success(new(existingDetails, "Sessão já preparada anteriormente."));
        }

        var selectedIds = command.Items.Where(x => x.Selected).Select(x => x.HabitId).Distinct().ToHashSet();
        if (selectedIds.Count == 0)
            return Result<RoutineSessionResult>.Failure("routine_session.empty", "Escolha ao menos um hábito para a sessão.");
        if (await sessions.GetActiveAsync(command.ClientId, command.UserId, ct) is { } active)
            return Result<RoutineSessionResult>.Success(new(active, "Já existe uma sessão ativa. Revise, retome, encerre ou cancele antes de preparar outra."));

        var ownedHabits = (await habits.ListActiveAsync(command.ClientId, command.UserId, ct))
            .Where(x => selectedIds.Contains(x.Id) && !x.IsPaused)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAt)
            .ToList();
        if (ownedHabits.Count == 0)
            return Result<RoutineSessionResult>.Failure("routine_session.no_available_habits", "Não há hábitos ativos disponíveis para esta sessão.");

        var now = clock.GetUtcNow().UtcDateTime;
        var session = new RoutineSession(Guid.NewGuid(), command.ClientId, command.UserId,
            string.IsNullOrWhiteSpace(command.Name) ? "Sessão de rotina" : command.Name.Trim(),
            null, "Habits", RoutineSessionStatuses.Prepared, timeZone.Today(), command.AvailableMinutes,
            command.ActiveAccountSessionId, null, null, command.IdempotencyKey, 1, now, now, null, null, null, null, null);
        var items = ownedHabits.Select((habit, index) => new RoutineSessionItem(Guid.NewGuid(), session.Id, habit.Id, habit.Name,
            habit.Category, index + 1, RoutineSessionItemStatuses.Pending, habit.TargetQuantity, habit.TargetUnit,
            habit.MinimumVersionQuantity, habit.MinimumVersionName, null, null, habit.EstimatedTimeMinutes, null, false, null, null, null, now)).ToList();

        await sessions.CreateAsync(session, items, command.CorrelationId, ct);
        await audit.LogAsync("routine_session.prepared", "Sessão de rotina preparada.", AuditSeverity.Info, command.UserId, null,
            new { sessionId = session.Id, items = items.Count, command.CorrelationId }, ct);
        var details = (await sessions.GetAsync(command.ClientId, command.UserId, session.Id, ct))!;
        return Result<RoutineSessionResult>.Success(new(details, "Sessão preparada. Iniciar não conclui hábitos automaticamente."));
    }

    public Task<RoutineSessionDetails?> GetAsync(Guid clientId, Guid userId, Guid sessionId, CancellationToken ct = default) =>
        sessions.GetAsync(clientId, userId, sessionId, ct);

    public Task<RoutineSessionDetails?> GetActiveAsync(Guid clientId, Guid userId, CancellationToken ct = default) =>
        sessions.GetActiveAsync(clientId, userId, ct);

    public Task<IReadOnlyList<RoutineSessionSummary>> HistoryAsync(Guid clientId, Guid userId, DateOnly? from, DateOnly? to, string? status, int page, int pageSize, CancellationToken ct = default) =>
        sessions.ListHistoryAsync(clientId, userId, from, to, status, Math.Max(0, page - 1) * Math.Clamp(pageSize, 1, 100), Math.Clamp(pageSize, 1, 100), ct);

    public Task<Result<RoutineSessionResult>> StartAsync(RoutineSessionCommand command, CancellationToken ct = default) =>
        TransitionAsync(command, RoutineSessionStatuses.Prepared, RoutineSessionStatuses.InProgress, "start", "Sessão iniciada.", ct);

    public Task<Result<RoutineSessionResult>> PauseAsync(RoutineSessionCommand command, CancellationToken ct = default) =>
        TransitionAsync(command, RoutineSessionStatuses.InProgress, RoutineSessionStatuses.Paused, "pause", "Sessão pausada. Nenhum tempo será registrado durante a ausência.", ct);

    public Task<Result<RoutineSessionResult>> ResumeAsync(RoutineSessionCommand command, CancellationToken ct = default) =>
        TransitionAsync(command, RoutineSessionStatuses.Paused, RoutineSessionStatuses.InProgress, "resume", "Sessão retomada do estado confirmado pelo servidor.", ct);

    public async Task<Result<RoutineSessionResult>> RecordMinimumAsync(RoutineSessionItemCommand command, CancellationToken ct = default)
    {
        var details = await sessions.GetAsync(command.ClientId, command.UserId, command.SessionId, ct);
        if (details is null) return Result<RoutineSessionResult>.Failure("routine_session.not_found", "Sessão não encontrada.");
        if (!string.Equals(details.Session.Status, RoutineSessionStatuses.InProgress, StringComparison.OrdinalIgnoreCase))
            return Result<RoutineSessionResult>.Failure("routine_session.not_in_progress", "Retome a sessão antes de registrar itens.");
        var item = details.Items.FirstOrDefault(x => x.Id == command.ItemId);
        if (item is null) return Result<RoutineSessionResult>.Failure("routine_session.item_not_found", "Item não encontrado.");
        if (string.Equals(item.Status, RoutineSessionItemStatuses.Completed, StringComparison.OrdinalIgnoreCase))
            return Result<RoutineSessionResult>.Success(new(details, "Este item já tinha alvo completo registrado."));

        await sessions.UpdateItemAsync(command.ClientId, command.UserId, command.SessionId, command.ItemId,
            RoutineSessionItemStatuses.MinimumReached, command.Quantity ?? item.MinimumQuantity, command.Unit ?? item.PlannedUnit,
            command.EffectiveMinutes, item.CompletionId, false, null, clock.GetUtcNow().UtcDateTime, ct);
        var refreshed = (await sessions.GetAsync(command.ClientId, command.UserId, command.SessionId, ct))!;
        await audit.LogAsync("routine_session.item.minimum_reached", "Versão mínima registrada na sessão.", AuditSeverity.Info,
            command.UserId, null, new { command.SessionId, command.ItemId, command.CorrelationId }, ct);
        return Result<RoutineSessionResult>.Success(new(refreshed, "Versão mínima registrada sem marcar alvo completo."));
    }

    public async Task<Result<RoutineSessionResult>> CompleteItemAsync(RoutineSessionItemCommand command, CancellationToken ct = default)
    {
        var details = await sessions.GetAsync(command.ClientId, command.UserId, command.SessionId, ct);
        if (details is null) return Result<RoutineSessionResult>.Failure("routine_session.not_found", "Sessão não encontrada.");
        if (!string.Equals(details.Session.Status, RoutineSessionStatuses.InProgress, StringComparison.OrdinalIgnoreCase))
            return Result<RoutineSessionResult>.Failure("routine_session.not_in_progress", "Retome a sessão antes de registrar itens.");
        var item = details.Items.FirstOrDefault(x => x.Id == command.ItemId);
        if (item is null) return Result<RoutineSessionResult>.Failure("routine_session.item_not_found", "Item não encontrado.");
        if (item.CompletionId.HasValue || string.Equals(item.Status, RoutineSessionItemStatuses.Completed, StringComparison.OrdinalIgnoreCase))
            return Result<RoutineSessionResult>.Success(new(details, "Este item já estava registrado como alvo completo."));

        var completion = await completeHabit.ExecuteAsync(new(command.ClientId, command.UserId, item.HabitId,
            details.Session.LocalDate, command.IdempotencyKey, "routine_session", command.CorrelationId), ct);
        if (completion.IsFailure) return Result<RoutineSessionResult>.Failure(completion.Error.Code, completion.Error.Message);

        await sessions.UpdateItemAsync(command.ClientId, command.UserId, command.SessionId, command.ItemId,
            RoutineSessionItemStatuses.Completed, command.Quantity ?? item.PlannedQuantity, command.Unit ?? item.PlannedUnit,
            command.EffectiveMinutes, null, false, null, clock.GetUtcNow().UtcDateTime, ct);
        var refreshed = (await sessions.GetAsync(command.ClientId, command.UserId, command.SessionId, ct))!;
        return Result<RoutineSessionResult>.Success(new(refreshed, "Alvo completo registrado pelo motor canônico de progresso."));
    }

    public async Task<Result<RoutineSessionResult>> SkipItemAsync(RoutineSessionItemCommand command, string? reason, CancellationToken ct = default)
    {
        var details = await sessions.GetAsync(command.ClientId, command.UserId, command.SessionId, ct);
        if (details is null) return Result<RoutineSessionResult>.Failure("routine_session.not_found", "Sessão não encontrada.");
        if (!string.Equals(details.Session.Status, RoutineSessionStatuses.InProgress, StringComparison.OrdinalIgnoreCase))
            return Result<RoutineSessionResult>.Failure("routine_session.not_in_progress", "Retome a sessão antes de pular itens.");
        await sessions.UpdateItemAsync(command.ClientId, command.UserId, command.SessionId, command.ItemId,
            RoutineSessionItemStatuses.Skipped, command.Quantity, command.Unit, command.EffectiveMinutes, null, true,
            string.IsNullOrWhiteSpace(reason) ? "Adiado pelo usuário" : reason.Trim(), clock.GetUtcNow().UtcDateTime, ct);
        var refreshed = (await sessions.GetAsync(command.ClientId, command.UserId, command.SessionId, ct))!;
        return Result<RoutineSessionResult>.Success(new(refreshed, "Item pulado nesta sessão; a ocorrência continua existindo na agenda."));
    }

    public async Task<Result<RoutineSessionResult>> CloseAsync(RoutineSessionCloseCommand command, CancellationToken ct = default)
    {
        var transition = await TransitionAsync(new(command.ClientId, command.UserId, command.SessionId, command.IdempotencyKey, command.CorrelationId),
            RoutineSessionStatuses.InProgress, RoutineSessionStatuses.Closed, "close", "Sessão encerrada parcialmente quando houver pendências.", ct);
        if (transition.IsFailure && transition.Error.Code == "routine_session.invalid_transition")
            transition = await TransitionAsync(new(command.ClientId, command.UserId, command.SessionId, command.IdempotencyKey, command.CorrelationId),
                RoutineSessionStatuses.Paused, RoutineSessionStatuses.Closed, "close", "Sessão encerrada parcialmente quando houver pendências.", ct);
        if (transition.IsFailure) return transition;
        var summary = BuildClosureSummary(transition.Value!.Details.Items);
        await sessions.SetClosureAsync(command.ClientId, command.UserId, command.SessionId, command.Reflection, summary, clock.GetUtcNow().UtcDateTime, ct);
        var refreshed = (await sessions.GetAsync(command.ClientId, command.UserId, command.SessionId, ct))!;
        return Result<RoutineSessionResult>.Success(new(refreshed, summary));
    }

    public Task<Result<RoutineSessionResult>> CancelAsync(RoutineSessionCommand command, CancellationToken ct = default) =>
        TransitionAnyActiveAsync(command, RoutineSessionStatuses.Canceled, "cancel", "Sessão cancelada. Registros já confirmados foram preservados.", ct);

    private async Task<Result<RoutineSessionResult>> TransitionAnyActiveAsync(RoutineSessionCommand command, string to, string action, string message, CancellationToken ct)
    {
        var details = await sessions.GetAsync(command.ClientId, command.UserId, command.SessionId, ct);
        if (details is null) return Result<RoutineSessionResult>.Failure("routine_session.not_found", "Sessão não encontrada.");
        if (RoutineSessionStatuses.Terminal.Contains(details.Session.Status))
            return Result<RoutineSessionResult>.Success(new(details, "A sessão já estava finalizada."));
        return await TransitionAsync(command, details.Session.Status, to, action, message, ct);
    }

    private async Task<Result<RoutineSessionResult>> TransitionAsync(RoutineSessionCommand command, string from, string to, string action, string message, CancellationToken ct)
    {
        var updated = await sessions.TransitionAsync(command.ClientId, command.UserId, command.SessionId, from, to, action,
            command.IdempotencyKey, command.CorrelationId, clock.GetUtcNow().UtcDateTime, ct);
        if (updated is null)
        {
            var current = await sessions.GetAsync(command.ClientId, command.UserId, command.SessionId, ct);
            if (current is null) return Result<RoutineSessionResult>.Failure("routine_session.not_found", "Sessão não encontrada.");
            if (string.Equals(current.Session.Status, to, StringComparison.OrdinalIgnoreCase))
                return Result<RoutineSessionResult>.Success(new(current, "Comando repetido; estado preservado pelo servidor."));
            return Result<RoutineSessionResult>.Failure("routine_session.invalid_transition", "A sessão mudou em outro dispositivo. Revise o estado confirmado pelo servidor.");
        }
        await audit.LogAsync($"routine_session.{action}", message, AuditSeverity.Info, command.UserId, null,
            new { command.SessionId, from, to, command.CorrelationId, command.IdempotencyKey }, ct);
        var details = (await sessions.GetAsync(command.ClientId, command.UserId, command.SessionId, ct))!;
        return Result<RoutineSessionResult>.Success(new(details, message));
    }

    private static string BuildClosureSummary(IReadOnlyList<RoutineSessionItem> items)
    {
        var completed = items.Count(x => x.Status == RoutineSessionItemStatuses.Completed);
        var minimum = items.Count(x => x.Status == RoutineSessionItemStatuses.MinimumReached);
        var skipped = items.Count(x => x.Status == RoutineSessionItemStatuses.Skipped);
        var pending = items.Count - completed - minimum - skipped;
        return $"Sessão encerrada: {completed} alvo(s) completo(s), {minimum} mínimo(s), {skipped} pulado(s) e {pending} pendente(s).";
    }
}
