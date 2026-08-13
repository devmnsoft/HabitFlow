using HabitFlow.Domain;

namespace HabitFlow.Application;

public sealed record DashboardNextAction(Guid? HabitId, string Title, string Description, string Url, string Label);
public sealed record DashboardGoal(Guid Id, string Title, int Percentage, string Recommendation);
public sealed record DashboardPlanAlert(string PlanCode, int ActiveHabits, int? Limit, bool IsNearLimit);
public sealed record DashboardSecurityAlert(bool IsVisible, string Message, string Url);
public sealed record DashboardWeekDay(DateOnly Date, int Planned, int Completed, decimal Percentage);
public sealed record DashboardExecutiveSummary(int ActiveHabits, int CompletedToday, int PendingToday,
    int ActiveGoals, decimal WeeklyPercentage, int CurrentStreak, int BestStreak,
    IReadOnlyList<DashboardWeekDay> Days, string Recommendation);
public sealed record DashboardViewModel(
    string Name, string Greeting, string LocalDate, DailyRoutinePlan Today,
    DashboardNextAction NextAction, DashboardGoal? PrimaryGoal, string WeeklyInsight,
    bool ShouldReviewWeek, DashboardPlanAlert Plan, DashboardSecurityAlert Security,
    DashboardExecutiveSummary Executive);

public sealed class DashboardNextActionService
{
    public DashboardNextAction Build(DailyRoutinePlan plan)
    {
        var next = plan.Items.FirstOrDefault(x => x.Status is DailyRoutineItemStatus.Available or DailyRoutineItemStatus.Upcoming);
        return next is null
            ? new(null, "Seu dia está organizado", "Você concluiu tudo que estava previsto. Aproveite o progresso.", "/my-day", "Ver meu dia")
            : new(next.HabitId, next.Name,
                next.PreferredTime is { } time ? $"Próximo passo sugerido para {time:HH\\:mm}." : "Um passo possível para fazer agora.",
                $"/habits/{next.HabitId}", "Abrir hábito");
    }
}

public sealed class DashboardInsightService
{
    public string Build(DailyRoutinePlan plan) => plan.Scheduled switch
    {
        0 => "Adicione um hábito para começar a acompanhar sua consistência.",
        _ when plan.Completed == plan.Scheduled => "Tudo concluído hoje. Seu ritmo está em dia.",
        _ when plan.Completed == 0 => $"Você tem {plan.Pending} {(plan.Pending == 1 ? "passo possível" : "passos possíveis")} para hoje.",
        _ => $"Você já concluiu {plan.Completed} de {plan.Scheduled} hábitos previstos hoje."
    };
}

public sealed class DashboardProgressService(DailyRoutinePlannerService planner)
{
    public Task<DailyRoutinePlan> BuildAsync(Guid clientId, Guid userId, DateOnly date, CancellationToken ct) =>
        planner.BuildAsync(new(clientId, userId, date), ct);
}

public sealed class DashboardPlanAlertService(IHabitRepository habits, PlanEntitlementService entitlements)
{
    public async Task<DashboardPlanAlert> BuildAsync(Guid clientId, Guid userId, CancellationToken ct)
    {
        var active = await habits.CountActiveAsync(clientId, userId, ct);
        var access = await entitlements.GetAccessSnapshotAsync(clientId, ct);
        var limit = await entitlements.GetIntegerFeatureAsync(userId, PlanFeatureCodes.ActiveHabitsLimit, ct);
        var near = limit is >= 0 && active >= Math.Max(1, limit.Value - 1);
        return new(access.EffectivePlanCode, active, limit is < 0 ? null : limit, near);
    }
}

public sealed class DashboardSecurityAlertService
{
    public DashboardSecurityAlert Build(User user) => user.RiskStatus == RiskStatus.Normal
        ? new(false, string.Empty, "/account/security")
        : new(true, "Há uma verificação de segurança pendente na sua conta.", "/account/security");
}

public sealed class DashboardOverviewService(
    IUserRepository users, IUserGoalRepository goals, DashboardProgressService progress,
    DashboardNextActionService nextAction, DashboardInsightService insights,
    DashboardPlanAlertService plans, DashboardSecurityAlertService security,
    ProgressSnapshotService snapshots, UserTimeZoneService timeZone, TimeProvider clock)
{
    public async Task<DashboardViewModel?> BuildAsync(Guid clientId, Guid userId, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(userId, ct);
        if (user is null || user.ClientId != clientId) return null;
        var zone = timeZone.Resolve();
        var localNow = TimeZoneInfo.ConvertTime(clock.GetUtcNow(), zone);
        var date = DateOnly.FromDateTime(localNow.DateTime);
        var today = await progress.BuildAsync(clientId, userId, date, ct);
        var userGoals = await goals.ListAsync(clientId, userId, ct);
        var primary = userGoals
            .Where(x => x.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.EndDate ?? DateOnly.MaxValue).ThenBy(x => x.CreatedAt).FirstOrDefault();
        DashboardGoal? goal = primary is null ? null : new(primary.Id, primary.Title,
            primary.TargetValue <= 0 ? 0 : Math.Clamp((int)Math.Round(primary.CurrentValue * 100d / primary.TargetValue), 0, 100),
            today.Pending > 0 ? "Concluir o próximo hábito é uma forma concreta de avançar esta semana." : "Revise os próximos passos do objetivo.");
        var greeting = localNow.Hour switch { < 12 => "Bom dia", < 18 => "Boa tarde", _ => "Boa noite" };
        var week = await snapshots.BuildPeriodAsync(clientId, userId, date.AddDays(-6), date, ct);
        var plan = await plans.BuildAsync(clientId, userId, ct);
        var recommendation = primary is not null && (await goals.ListLinkedHabitsAsync(primary.Id, clientId, userId, ct)).Count == 0
            ? "Vincule um hábito para transformar seu objetivo em ação diária."
            : today.Completed == 0 && today.Scheduled > 0
                ? "Comece pelo menor hábito para ganhar ritmo."
                : plan.IsNearLimit
                    ? "Você está perto do limite de hábitos ativos do plano atual."
                    : "Proteja o seu ritmo: escolha uma próxima ação pequena e possível.";
        var activeGoals = userGoals.Count(x => x.Status.Equals("Active", StringComparison.OrdinalIgnoreCase));
        var executive = new DashboardExecutiveSummary(plan.ActiveHabits, today.Completed, today.Pending, activeGoals,
            week.Percentage, week.CurrentStreak, week.BestStreak,
            week.DailySummaries.Select(x => new DashboardWeekDay(x.Date, x.Scheduled, x.Completed, x.Percentage)).ToList(), recommendation);
        return new(user.Name, greeting, date.ToString("dddd, dd 'de' MMMM"), today,
            nextAction.Build(today), goal, insights.Build(today), localNow.DayOfWeek is DayOfWeek.Sunday,
            plan, security.Build(user), executive);
    }
}
