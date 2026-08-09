using HabitFlow.Domain;
using HabitFlow.Shared;

namespace HabitFlow.Application;

public sealed record GoalProgressViewModel(int CurrentValue,int TargetValue,int Percentage,string AccessibleLabel);
public sealed record GoalRecommendationViewModel(string Title,string Explanation,string Action,bool IsBlocked);
public sealed record GoalCardViewModel(UserGoal Goal,GoalProgressViewModel Progress,int LinkedHabitCount);
public sealed record GoalListViewModel(IReadOnlyList<GoalCardViewModel> Goals,string? Search,string Status,int ActiveCount,int PausedCount,int CompletedCount);
public sealed record GoalDetailsViewModel(UserGoal Goal,GoalProgressViewModel Progress,IReadOnlyList<Habit> LinkedHabits,IReadOnlyList<Habit> AvailableHabits,IReadOnlyList<GoalTimelineEntry> Timeline,GoalRecommendationViewModel Recommendation,string? Blocker);
public sealed record GoalEditorViewModel(UserGoal? Goal,DateOnly DefaultStartDate);

public sealed class GoalProgressService
{
 public GoalProgressViewModel Build(UserGoal goal){var percentage=goal.TargetValue<=0?0:(int)Math.Clamp((long)goal.CurrentValue*100/goal.TargetValue,0,100);return new(goal.CurrentValue,goal.TargetValue,percentage,$"{percentage}% concluído: {goal.CurrentValue} de {goal.TargetValue}");}
}

public sealed class GoalInsightsService
{
 public string? FindBlocker(UserGoal goal,IReadOnlyList<Habit> habits){if(goal.Status=="Paused")return "O objetivo está pausado. Retome quando houver espaço na rotina.";if(habits.Count==0&&goal.TargetType!="Custom")return "Nenhum hábito está conectado a este objetivo.";if(habits.All(h=>h.IsArchived||h.IsPaused))return "Todos os hábitos conectados estão pausados ou arquivados.";return null;}
}

public sealed class GoalRecommendationService
{
 public GoalRecommendationViewModel Build(UserGoal goal,IReadOnlyList<Habit> habits,string? blocker){if(blocker is not null)return new("Remova o bloqueio",blocker,habits.Count==0?"Conecte um hábito que caiba na sua semana.":"Retome um hábito conectado.",true);var active=habits.Where(h=>!h.IsArchived&&!h.IsPaused).ToList();var weekly=active.Sum(h=>h.TargetPerWeek??(h.FrequencyType==HabitFrequencyType.Daily?7:1));return weekly>0?new("O que fazer esta semana",$"Seus {active.Count} hábitos ativos somam {weekly} ações planejadas por semana.","Priorize a próxima ação e registre cada conclusão.",false):new("Defina a próxima ação","Transforme a direção do objetivo em um comportamento observável.","Conecte seu primeiro hábito.",false);}
}

public sealed class GoalQueryService(IUserGoalRepository goals,IHabitRepository habits,GoalProgressService progress,GoalInsightsService insights,GoalRecommendationService recommendations)
{
 public async Task<GoalListViewModel> ListAsync(Guid c,Guid u,string? search,string? status,CancellationToken ct=default){var all=await goals.ListAsync(c,u,ct);var normalizedStatus=string.IsNullOrWhiteSpace(status)?"All":status;var filtered=all.Where(g=>(normalizedStatus=="All"||g.Status.Equals(normalizedStatus,StringComparison.OrdinalIgnoreCase))&&(string.IsNullOrWhiteSpace(search)||g.Title.Contains(search,StringComparison.OrdinalIgnoreCase)||(g.Description?.Contains(search,StringComparison.OrdinalIgnoreCase)??false))).ToList();var cards=new List<GoalCardViewModel>();foreach(var goal in filtered){var linked=await goals.ListLinkedHabitsAsync(goal.Id,c,u,ct);cards.Add(new(goal,progress.Build(goal),linked.Count));}return new(cards,search,normalizedStatus,all.Count(g=>g.Status=="Active"),all.Count(g=>g.Status=="Paused"),all.Count(g=>g.Status=="Completed"));}
 public async Task<GoalDetailsViewModel?> GetAsync(Guid id,Guid c,Guid u,CancellationToken ct=default){var goal=await goals.GetAsync(id,c,u,ct);if(goal is null)return null;var linked=await goals.ListLinkedHabitsAsync(id,c,u,ct);var linkedIds=linked.Select(h=>h.Id).ToHashSet();var available=(await habits.ListActiveAsync(c,u,ct)).Where(h=>!linkedIds.Contains(h.Id)).ToList();var blocker=insights.FindBlocker(goal,linked);return new(goal,progress.Build(goal),linked,available,await goals.ListTimelineAsync(id,c,u,ct),recommendations.Build(goal,linked,blocker),blocker);}
}

public sealed class GoalLinkedHabitService(IUserGoalRepository goals,IHabitRepository habits,AuditService audit)
{
 public async Task<Result> LinkAsync(Guid goalId,Guid habitId,Guid c,Guid u,CancellationToken ct=default)=>await Change(goalId,habitId,c,u,true,ct);
 public async Task<Result> UnlinkAsync(Guid goalId,Guid habitId,Guid c,Guid u,CancellationToken ct=default)=>await Change(goalId,habitId,c,u,false,ct);
 async Task<Result> Change(Guid goalId,Guid habitId,Guid c,Guid u,bool link,CancellationToken ct){if(await goals.GetAsync(goalId,c,u,ct) is null||await habits.GetAsync(c,u,habitId,ct) is null)return Result.Failure("goal.link.not_found","Objetivo ou hábito não encontrado.");if(link)await goals.LinkHabitAsync(goalId,habitId,c,u,ct);else await goals.UnlinkHabitAsync(goalId,habitId,c,u,ct);await audit.LogAsync(link?"goal.habit.linked":"goal.habit.unlinked",link?"Hábito vinculado ao objetivo.":"Hábito desvinculado do objetivo.",userId:u,metadata:new{goalId,habitId,clientId=c},ct:ct);return Result.Success();}
}

public sealed class GoalLifecycleService(IUserGoalRepository goals,AuditService audit)
{
 static readonly IReadOnlyDictionary<string,string[]> Allowed=new Dictionary<string,string[]>(StringComparer.Ordinal){["Active"]=["Paused","Completed","Canceled"],["Paused"]=["Active","Canceled"],["Completed"]=[],["Canceled"]=[]};
 public async Task<Result> ChangeAsync(Guid id,Guid c,Guid u,string status,CancellationToken ct=default){var goal=await goals.GetAsync(id,c,u,ct);if(goal is null)return Result.Failure("goal.not_found","Objetivo não encontrado.");if(!Allowed.TryGetValue(goal.Status,out var transitions)||!transitions.Contains(status))return Result.Failure("goal.transition.invalid","Esta mudança não está disponível para o estado atual.");await goals.SetStatusAsync(id,c,u,status,ct);await audit.LogAsync($"goal.{status.ToLowerInvariant()}","Ciclo de vida do objetivo atualizado.",userId:u,metadata:new{id,clientId=c,from=goal.Status,to=status},ct:ct);return Result.Success();}
}
