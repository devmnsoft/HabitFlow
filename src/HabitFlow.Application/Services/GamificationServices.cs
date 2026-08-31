using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed record AchievementEvaluationContext(int TotalCompletions, int CurrentStreak, bool GoalCompleted,
    bool FirstHabitCreated=false, bool RoutineCompleted=false, bool ConsistentWeek=false, bool ReturnedAfterPause=false, bool UsedTemplate=false);

public sealed class AchievementEvaluator
{
    public IReadOnlyList<string> Evaluate(AchievementEvaluationContext context)
    {
        var codes = new List<string>();
        if (context.TotalCompletions >= 1) codes.Add("first_completion");
        if (context.CurrentStreak >= 3) codes.Add("consistency_3");
        if (context.CurrentStreak >= 7) codes.Add("consistency_7");
        if (context.CurrentStreak >= 30) codes.Add("consistency_30");
        if (context.TotalCompletions >= 30) codes.Add("total_30");
        if (context.GoalCompleted) codes.Add("weekly_goal_completed");
        if (context.FirstHabitCreated) codes.Add("first_habit");
        if (context.RoutineCompleted) codes.Add("routine_completed");
        if (context.ConsistentWeek) codes.Add("consistent_week");
        if (context.ReturnedAfterPause) codes.Add("return_after_pause");
        if (context.UsedTemplate) codes.Add("template_used");
        return codes;
    }
}

public sealed record LeaderboardPage(LeaderboardPreference Preference, PointsBalance Balance, IReadOnlyList<LeaderboardEntry> Entries);
public sealed class HealthyPointsService(IGamificationRepository repository, UserTimeZoneService clock, AuditService audit, ILogger<HealthyPointsService> logger)
{
    public async Task<int> GrantCompletionAsync(Guid clientId,Guid userId,Guid completionId,int streak,CancellationToken ct=default)
    { var points=await repository.GrantPointsAsync(clientId,userId,completionId,10+(streak>=7?5:0),clock.Today(),DateTime.UtcNow,ct); var action=points>0?"points.granted":"gamification.limit_reached"; await audit.LogAsync(action,"Movimentação saudável de pontos",userId:userId,metadata:new{completionId,points},ct:ct); logger.LogInformation("{Event} ClientId={ClientId} UserId={UserId} Points={Points}",action,clientId,userId,points); return points; }
    public async Task<int> RevertCompletionAsync(Guid clientId,Guid userId,Guid completionId,CancellationToken ct=default)
    { var points=await repository.RevertPointsAsync(clientId,userId,completionId,DateTime.UtcNow,ct); if(points<0) await audit.LogAsync("points.reverted","Pontos de conclusão revertidos",userId:userId,metadata:new{completionId,points},ct:ct); return points; }
}
public sealed class LeaderboardService(IGamificationRepository repository,UserTimeZoneService clock,AuditService audit)
{
    public async Task<LeaderboardPage> GetAsync(Guid clientId,Guid userId,CancellationToken ct=default){var p=await repository.GetLeaderboardPreferenceAsync(clientId,userId,ct)??new(clientId,userId,false,LeaderboardScope.Private,"Participante",null,DateTime.UtcNow);var entries=p.IsOptedIn?await repository.ListLeaderboardAsync(clientId,userId,p.Scope,ct):[];return new(p,await repository.GetPointsAsync(clientId,userId,clock.Today(),ct),entries);}
    public async Task SaveAsync(Guid clientId,Guid userId,bool optedIn,LeaderboardScope scope,string publicName,CancellationToken ct=default){var safe=string.IsNullOrWhiteSpace(publicName)?"Participante":publicName.Trim();if(safe.Length>40)safe=safe[..40];await repository.SaveLeaderboardPreferenceAsync(new(clientId,userId,optedIn,optedIn?scope:LeaderboardScope.Private,safe,null,DateTime.UtcNow),ct);await audit.LogAsync(optedIn?"leaderboard.joined":"leaderboard.left","Preferência do ranking atualizada",userId:userId,metadata:new{scope},ct:ct);}
}

public sealed class AchievementNotificationService(INotificationRepository notifications)
{
    public Task NotifyAsync(Guid userId, string code, CancellationToken ct = default) => notifications.CreateAsync(
        new(Guid.NewGuid(), userId, "achievement", "Mais um passo concluído", Message(code), "success", false,
            "/achievements", "achievement", null, DateTime.UtcNow, null), ct);
    private static string Message(string code) => code switch
    {
        "consistency_3" => "Boa sequência esta semana.",
        "consistency_7" => "Sete dias presentes, no seu ritmo.",
        "weekly_goal_completed" => "Sua meta semanal foi alcançada.",
        _ => "Hoje conta. Amanhã continua."
    };
}

public sealed class AchievementService(IGamificationRepository repository, AchievementEvaluator evaluator,
    AchievementNotificationService notifications, ILogger<AchievementService> logger)
{
    public async Task<IReadOnlyList<string>> EvaluateCompletionAsync(Guid clientId, Guid userId, int streak,
        bool goalCompleted, CancellationToken ct = default)
    {
        var total = await repository.CountCompletionsAsync(clientId, userId, ct);
        var unlocked = new List<string>();
        foreach (var code in evaluator.Evaluate(new(total, streak, goalCompleted)))
            if (await repository.UnlockAsync(clientId, userId, code, DateTime.UtcNow, ct))
            {
                unlocked.Add(code);
                await notifications.NotifyAsync(userId, code, ct);
                logger.LogInformation("gamification.achievement.unlocked {ClientId} {UserId} {AchievementCode}", clientId, userId, code);
            }
        return unlocked;
    }
}

public sealed class GamificationService(IGamificationRepository repository, FeatureAccessService access,
    UserTimeZoneService clock, ILogger<GamificationService> logger)
{
    public async Task<Result<WeeklyGoal>> CreateWeeklyGoalAsync(Guid clientId, Guid userId, string name, int target,
        IReadOnlyCollection<Guid> habitIds, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 120 || target is < 1 or > 100 || habitIds.Count == 0)
            return Result<WeeklyGoal>.Failure("weekly_goal.invalid", "Escolha um nome, uma meta entre 1 e 100 e ao menos um hábito.");
        var today = clock.Today();
        var mondayOffset = ((int)today.DayOfWeek + 6) % 7;
        var start = today.AddDays(-mondayOffset);
        var goal = new WeeklyGoal(Guid.NewGuid(), clientId, userId, name.Trim(), start, start.AddDays(6), target, 0, "Active", DateTime.UtcNow, null);
        var created = await repository.CreateWeeklyGoalAsync(goal, habitIds, ct);
        if (created is null) return Result<WeeklyGoal>.Failure("weekly_goal.duplicate", "Esta meta já está acompanhando sua semana.");
        logger.LogInformation("gamification.goal.created {ClientId} {UserId} {GoalId}", clientId, userId, created.Id);
        return Result<WeeklyGoal>.Success(created);
    }
    public Task<IReadOnlyList<WeeklyGoal>> GoalsAsync(Guid clientId, Guid userId, CancellationToken ct = default) => repository.ListWeeklyGoalsAsync(clientId,userId,ct);
    public async Task<GamificationSnapshot> SnapshotAsync(Guid clientId, Guid userId, int currentStreak, int bestStreak, CancellationToken ct = default) =>
        new(await repository.ListWeeklyGoalsAsync(clientId,userId,ct), await repository.ListAchievementsAsync(clientId,userId,ct),
            await repository.ListLockedDefinitionsAsync(clientId,userId,ct), await repository.CountCompletionsAsync(clientId,userId,ct),currentStreak,bestStreak);
    public async Task<Result<bool>> UseFreezeAsync(Guid clientId, Guid userId, Guid habitId, DateOnly date, string? reason, CancellationToken ct = default)
    {
        var featureAccess = await access.CheckFeatureAsync(userId,"streak_freeze",ct);
        if (!featureAccess.Allowed)
        { logger.LogWarning("gamification.plan_blocked {ClientId} {UserId} {FeatureCode}",clientId,userId,"streak_freeze"); return Result<bool>.Failure("plan.feature_locked","A proteção de sequência faz parte do plano Ritmo. Seus dados reais permanecem iguais."); }
        if(date>clock.Today()) return Result<bool>.Failure("freeze.future","A proteção não pode ser usada em uma data futura.");
        var used=await repository.UseFreezeAsync(new(Guid.NewGuid(),clientId,userId,habitId,date,string.IsNullOrWhiteSpace(reason)?null:reason.Trim(),DateTime.UtcNow),ct);
        if(!used)return Result<bool>.Failure("freeze.duplicate","Este dia já está protegido para esse hábito.");
        logger.LogInformation("gamification.streak_freeze.used {ClientId} {UserId} {HabitId} {Date}",clientId,userId,habitId,date);
        return Result<bool>.Success(true);
    }
}
