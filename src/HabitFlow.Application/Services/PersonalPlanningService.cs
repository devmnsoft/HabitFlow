using HabitFlow.Domain;

namespace HabitFlow.Application;

public sealed record PriorityConfigurationItem(Guid GoalId, int PriorityOrder, int? WeeklyAvailableMinutes);
public sealed record PriorityConfigurationResult(IReadOnlyList<PersonalPriorityDetails> Priorities, IReadOnlyList<UserGoal> ActiveGoals);

public sealed record NextStepSuggestion(string Key, string Title, string Reason, string Source, string DestinationUrl, string ActionLabel, bool CanDismiss);

public sealed record MonthlyReviewMetric(string Label, string Value, string Explanation);
public sealed record MonthlyReviewPlanOption(string Code, string Title, string Description, bool Confirmed);
public sealed record MonthlyReviewResult(
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    bool IsCurrentMonth,
    int Scheduled,
    int Completed,
    int MinimumReached,
    int FullCompletions,
    int SessionCount,
    int PauseCount,
    int GoalCount,
    int SmallSampleThreshold,
    IReadOnlyList<MonthlyReviewMetric> Metrics,
    IReadOnlyList<WeeklyReviewHabitResult> Habits,
    IReadOnlyList<WeeklyReviewGoalResult> Goals,
    IReadOnlyList<MonthlyReviewPlanOption> PlanOptions,
    MonthlyReview? Draft,
    string Summary,
    string AiStatus);

public sealed class PersonalPriorityService(IPersonalPlanningRepository planning, IUserGoalRepository goals, TimeProvider clock)
{
    public async Task<PriorityConfigurationResult> BuildAsync(Guid clientId, Guid userId, DateOnly today, CancellationToken ct = default)
    {
        var priorities = await planning.ListPrioritiesAsync(clientId, userId, today, ct);
        var activeGoals = (await goals.ListAsync(clientId, userId, ct)).Where(x => x.Status.Equals("Active", StringComparison.OrdinalIgnoreCase)).ToList();
        return new(priorities, activeGoals);
    }

    public async Task SaveAsync(Guid clientId, Guid userId, IReadOnlyList<PriorityConfigurationItem> items, DateOnly effectiveFrom, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var clean = items
            .Where(x => x.GoalId != Guid.Empty)
            .GroupBy(x => x.GoalId)
            .Select(x => x.First())
            .OrderBy(x => x.PriorityOrder)
            .Take(5)
            .Select((x, index) => new PersonalPriority(Guid.NewGuid(), clientId, userId, x.GoalId, index + 1,
                x.WeeklyAvailableMinutes is > 0 and <= 10080 ? x.WeeklyAvailableMinutes : null,
                effectiveFrom, null, 1, now, now))
            .ToList();
        await planning.SavePrioritiesAsync(clientId, userId, clean, ct);
    }
}

public sealed class NextStepService(
    IPersonalPlanningRepository planning,
    IRoutineSessionRepository sessions,
    DailyRoutinePlannerService daily,
    IUserGoalRepository goals)
{
    public async Task<IReadOnlyList<NextStepSuggestion>> BuildAsync(Guid clientId, Guid userId, DateOnly today, CancellationToken ct = default)
    {
        var dismissed = await planning.ListDismissedSuggestionKeysAsync(clientId, userId, today, ct);
        var suggestions = new List<NextStepSuggestion>();
        var activeSession = await sessions.GetActiveAsync(clientId, userId, ct);
        if (activeSession is not null)
        {
            suggestions.Add(new($"session:{activeSession.Session.Id}", "Continuar sessão de rotina",
                $"Sessão \"{activeSession.Session.Name}\" está {activeSession.Session.Status.ToLowerInvariant()} e preserva seu progresso entre dispositivos.",
                "routine_sessions", $"/routine-sessions/{activeSession.Session.Id}", "Abrir sessão", true));
        }

        var plan = await daily.BuildAsync(new(clientId, userId, today), ct);
        var priorities = await planning.ListPrioritiesAsync(clientId, userId, today, ct);
        var priorityHabitIds = priorities.SelectMany(x => x.LinkedHabits).Select(x => x.Id).ToHashSet();
        var nextHabit = plan.Items.FirstOrDefault(x => x.Status is DailyRoutineItemStatus.Available or DailyRoutineItemStatus.Upcoming && priorityHabitIds.Contains(x.HabitId))
            ?? plan.Items.FirstOrDefault(x => x.Status is DailyRoutineItemStatus.Available or DailyRoutineItemStatus.Upcoming);
        if (nextHabit is not null)
        {
            suggestions.Add(new($"habit:{nextHabit.HabitId}:{today:yyyyMMdd}", $"Registrar {nextHabit.Name}",
                priorityHabitIds.Contains(nextHabit.HabitId) ? "Hábito vinculado a uma prioridade ativa do período." : "Hábito previsto na agenda canônica de hoje.",
                "daily_routine", "/my-day", "Ir para Meu dia", true));
        }

        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var previousMonthStart = monthStart.AddMonths(-1);
        var previousReview = await planning.GetMonthlyReviewAsync(clientId, userId, previousMonthStart, ct);
        if (previousReview is null || !previousReview.Status.Equals(MonthlyReviewStatuses.Completed, StringComparison.OrdinalIgnoreCase))
        {
            suggestions.Add(new($"monthly-review:{previousMonthStart:yyyy-MM}", "Concluir revisão mensal",
                "O mês anterior terminou e ainda não tem revisão concluída. O rascunho pode ser salvo sem alterar hábitos.",
                "monthly_review", $"/monthly-review/{previousMonthStart:yyyy-MM}", "Revisar mês", true));
        }

        var activeGoals = (await goals.ListAsync(clientId, userId, ct)).Where(x => x.Status.Equals("Active", StringComparison.OrdinalIgnoreCase)).ToList();
        if (activeGoals.Count > 0 && priorities.Count == 0)
        {
            suggestions.Add(new("priorities:configure", "Escolher prioridades do período",
                "Há objetivos ativos, mas nenhuma prioridade configurada para orientar a apresentação do dia.",
                "priorities", "/priorities", "Definir prioridades", true));
        }

        return suggestions
            .Where(x => !dismissed.Contains(x.Key))
            .GroupBy(x => x.Key)
            .Select(x => x.First())
            .Take(4)
            .ToList();
    }

    public Task DismissAsync(Guid clientId, Guid userId, string key, DateOnly today, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 160) throw new ArgumentException("Sugestão inválida.");
        return planning.DismissSuggestionAsync(clientId, userId, key.Trim(), today, ct);
    }
}

public sealed class MonthlyReviewService(
    DailyRoutinePlannerService daily,
    IHabitCompletionRepository completions,
    IRoutineSessionRepository sessions,
    IHabitScheduleExceptionRepository exceptions,
    IPersonalPlanningRepository planning,
    IUserGoalRepository goals,
    TimeProvider clock,
    UserTimeZoneService timeZone)
{
    public async Task<MonthlyReviewResult> BuildAsync(Guid clientId, Guid userId, DateOnly periodStart, CancellationToken ct = default)
    {
        periodStart = new DateOnly(periodStart.Year, periodStart.Month, 1);
        var today = timeZone.Today();
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);
        var observedEnd = periodEnd > today ? today : periodEnd;
        var dailyPlans = new List<DailyRoutinePlan>();
        for (var day = periodStart; day <= observedEnd; day = day.AddDays(1))
            dailyPlans.Add(await daily.BuildAsync(new(clientId, userId, day), ct));

        var scheduled = dailyPlans.Sum(x => x.Scheduled);
        var completed = dailyPlans.Sum(x => x.Completed);
        var habitRows = dailyPlans.SelectMany(x => x.Items)
            .Where(x => x.Status is not DailyRoutineItemStatus.Excused and not DailyRoutineItemStatus.Moved)
            .GroupBy(x => x.HabitId)
            .Select(g =>
            {
                var first = g.First();
                var total = g.Count();
                var done = g.Count(x => x.Status == DailyRoutineItemStatus.Completed);
                return new WeeklyReviewHabitResult(first.HabitId, first.Name, first.Category ?? "Outras", total, done,
                    total == 0 ? 0 : (int)Math.Round(done * 100d / total),
                    total < 3 ? "Amostra pequena: observe antes de mudar a rotina." : done >= total * 0.7 ? "Ritmo consistente no período." : "Pode valer um ajuste pequeno no próximo ciclo.",
                    first.EstimatedMinutes > 0 ? first.EstimatedMinutes : null,
                    false,
                    0);
            })
            .Where(x => x.Scheduled > 0)
            .OrderByDescending(x => x.Percentage)
            .ThenBy(x => x.Name)
            .ToList();

        var completionRows = await completions.ListAsync(clientId, userId, periodStart, observedEnd, ct);
        var sessionRows = await sessions.ListHistoryAsync(clientId, userId, periodStart, observedEnd, null, 0, 200, ct);
        var pauseRows = await exceptions.ListAsync(clientId, userId, periodStart, observedEnd, ct);
        var goalRows = (await goals.ListAsync(clientId, userId, ct))
            .Where(x => x.Status.Equals("Active", StringComparison.OrdinalIgnoreCase) || (x.CompletedAt.HasValue && DateOnly.FromDateTime(x.CompletedAt.Value) >= periodStart))
            .Select(x => new WeeklyReviewGoalResult(x.Id, x.Title, x.Status, x.TargetValue <= 0 ? 0 : Math.Round(Math.Clamp(x.CurrentValue * 100m / x.TargetValue, 0, 100), 1), false))
            .OrderByDescending(x => x.Percentage)
            .ToList();
        var draft = await planning.GetMonthlyReviewAsync(clientId, userId, periodStart, ct);
        var percentage = scheduled == 0 ? 0 : (int)Math.Round(completed * 100d / scheduled);
        var metrics = new List<MonthlyReviewMetric>
        {
            new("Consistência", scheduled == 0 ? "sem dados" : $"{percentage}%", $"Denominador: {scheduled} ocorrências previstas entre {periodStart:dd/MM} e {observedEnd:dd/MM}."),
            new("Conclusões completas", completionRows.Count.ToString(), "Fonte: registros canônicos de conclusão; check-ins repetidos não são somados."),
            new("Sessões", sessionRows.Count.ToString(), "Fonte: sessões de rotina fechadas, pausadas, canceladas ou preparadas no período."),
            new("Pausas e mudanças", pauseRows.Count.ToString(), "Fonte: exceções explícitas de agenda; dispensa não apaga a ocorrência histórica.")
        };
        var options = BuildPlanOptions(habitRows, draft);
        var summary = scheduled == 0
            ? "Não havia ocorrências suficientes para uma leitura mensal. Configure prioridades ou mantenha uma rotina simples para gerar base."
            : $"{completed} de {scheduled} ocorrências foram concluídas. {(periodEnd > today ? "Este mês ainda está em andamento; trate os números como parciais." : "O mês está encerrado; as comparações seguem descritivas, sem causalidade.")}";
        return new(periodStart, periodEnd, periodEnd > today, scheduled, completed, 0, completionRows.Count, sessionRows.Count,
            pauseRows.Count, goalRows.Count, 3, metrics, habitRows, goalRows, options, draft, summary,
            "IA externa não foi chamada nesta fatia; resumo e propostas foram gerados por regras determinísticas.");
    }

    public async Task<MonthlyReview> SaveDraftAsync(Guid clientId, Guid userId, DateOnly periodStart, string? reflection, string? selectedNextStep, int version, string idempotencyKey, CancellationToken ct = default) =>
        await SaveAsync(clientId, userId, periodStart, MonthlyReviewStatuses.Draft, reflection, selectedNextStep, null, version, idempotencyKey, ct);

    public async Task<MonthlyReview> CompleteAsync(Guid clientId, Guid userId, DateOnly periodStart, string? reflection, string? selectedNextStep, string? confirmedPlanCode, int version, string idempotencyKey, CancellationToken ct = default) =>
        await SaveAsync(clientId, userId, periodStart, MonthlyReviewStatuses.Completed, reflection, selectedNextStep, confirmedPlanCode, version, idempotencyKey, ct);

    private async Task<MonthlyReview> SaveAsync(Guid clientId, Guid userId, DateOnly periodStart, string status, string? reflection, string? selectedNextStep, string? confirmedPlanCode, int version, string idempotencyKey, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey)) throw new ArgumentException("Chave de idempotência obrigatória.");
        periodStart = new DateOnly(periodStart.Year, periodStart.Month, 1);
        var now = clock.GetUtcNow().UtcDateTime;
        var existing = await planning.GetMonthlyReviewAsync(clientId, userId, periodStart, ct);
        var review = new MonthlyReview(existing?.Id ?? Guid.NewGuid(), clientId, userId, periodStart, periodStart.AddMonths(1).AddDays(-1),
            status, reflection?.Trim(), selectedNextStep?.Trim(), confirmedPlanCode?.Trim(), existing?.IdempotencyKey ?? idempotencyKey.Trim(),
            existing?.Version ?? 1, existing?.CreatedAt ?? now, now, status == MonthlyReviewStatuses.Completed ? now : existing?.CompletedAt);
        return await planning.UpsertMonthlyReviewAsync(review, version <= 0 ? existing?.Version ?? 0 : version, ct);
    }

    private static IReadOnlyList<MonthlyReviewPlanOption> BuildPlanOptions(IReadOnlyList<WeeklyReviewHabitResult> habits, MonthlyReview? draft)
    {
        var attention = habits.OrderBy(x => x.Percentage).ThenByDescending(x => x.Scheduled).FirstOrDefault();
        var options = new[]
        {
            new MonthlyReviewPlanOption("keep_routine", "Manter rotina", "Preserva agenda e metas atuais para observar mais um ciclo.", draft?.ConfirmedPlanCode == "keep_routine"),
            new MonthlyReviewPlanOption("reduce_frequency", "Reduzir frequência", attention is null ? "Sem hábito suficiente para ajuste." : $"Avaliar redução futura de {attention.Name}; nada muda sem edição explícita do hábito.", draft?.ConfirmedPlanCode == "reduce_frequency"),
            new MonthlyReviewPlanOption("adjust_minimum", "Ajustar versão mínima", "Revisar versão mínima para facilitar retomadas sem registrar alvo completo automaticamente.", draft?.ConfirmedPlanCode == "adjust_minimum"),
            new MonthlyReviewPlanOption("start_experiment", "Iniciar experimento", "Criar um teste pequeno e reversível para o próximo ciclo.", draft?.ConfirmedPlanCode == "start_experiment")
        };
        return options;
    }
}
