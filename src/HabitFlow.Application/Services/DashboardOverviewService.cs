using HabitFlow.Domain;

namespace HabitFlow.Application;

public sealed record DashboardNextAction(Guid? HabitId, string Title, string Description, string Url, string Label);
public sealed record DashboardGoal(Guid Id, string Title, int Percentage, string Recommendation);
public sealed record DashboardPlanAlert(string PlanCode, int ActiveHabits, int? Limit, bool IsNearLimit);
public sealed record DashboardSecurityAlert(bool IsVisible, string Message, string Url);
public sealed record DashboardViewModel(
    string Name, string Greeting, string LocalDate, DailyRoutinePlan Today,
    DashboardNextAction NextAction, DashboardGoal? PrimaryGoal, string WeeklyInsight,
    bool ShouldReviewWeek, DashboardPlanAlert Plan, DashboardSecurityAlert Security);

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
    UserTimeZoneService timeZone, TimeProvider clock)
{
    public async Task<DashboardViewModel?> BuildAsync(Guid clientId, Guid userId, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(userId, ct);
        if (user is null || user.ClientId != clientId) return null;
        var zone = timeZone.Resolve();
        var localNow = TimeZoneInfo.ConvertTime(clock.GetUtcNow(), zone);
        var date = DateOnly.FromDateTime(localNow.DateTime);
        var today = await progress.BuildAsync(clientId, userId, date, ct);
        var primary = (await goals.ListAsync(clientId, userId, ct))
            .Where(x => x.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.EndDate ?? DateOnly.MaxValue).ThenBy(x => x.CreatedAt).FirstOrDefault();
        DashboardGoal? goal = primary is null ? null : new(primary.Id, primary.Title,
            primary.TargetValue <= 0 ? 0 : Math.Clamp((int)Math.Round(primary.CurrentValue * 100d / primary.TargetValue), 0, 100),
            today.Pending > 0 ? "Concluir o próximo hábito é uma forma concreta de avançar esta semana." : "Revise os próximos passos do objetivo.");
        var greeting = localNow.Hour switch { < 12 => "Bom dia", < 18 => "Boa tarde", _ => "Boa noite" };
        return new(user.Name, greeting, date.ToString("dddd, dd 'de' MMMM"), today,
            nextAction.Build(today), goal, insights.Build(today), localNow.DayOfWeek is DayOfWeek.Sunday,
            await plans.BuildAsync(clientId, userId, ct), security.Build(user));
    }
}
