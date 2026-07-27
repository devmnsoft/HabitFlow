using HabitFlow.Domain;
using HabitFlow.Shared;
namespace HabitFlow.Application;
public sealed class GoalService(IUserGoalRepository goals,FeatureAccessService access)
{
 static readonly HashSet<string> TargetTypes = new(StringComparer.Ordinal) { "HabitCompletions", "ActiveDays", "StreakDays", "WeeklyCompletions", "Custom" };
 public Task<IReadOnlyList<UserGoal>> ListAsync(Guid clientId,Guid userId,CancellationToken ct=default)=>goals.ListAsync(clientId,userId,ct);
 public Task<UserGoal?> GetAsync(Guid id,Guid clientId,Guid userId,CancellationToken ct=default)=>goals.GetAsync(id,clientId,userId,ct);
 public async Task<Result<UserGoal>> CreateAsync(Guid clientId,Guid userId,string title,string? description,string targetType,int targetValue,DateOnly startDate,DateOnly? endDate,CancellationToken ct=default){
  var limit=await access.GetLimitAsync(userId,PlanFeatureCodes.ActiveGoalsLimit,ct); var active=await goals.CountActiveAsync(clientId,userId,ct);
  if(limit is >=0 && active>=limit)return Result<UserGoal>.Failure("goal.limit","Seu limite de objetivos ativos foi alcançado. Os objetivos que você já criou continuam aqui.");
  if(!IsValid(title,targetType,targetValue,startDate,endDate))return Result<UserGoal>.Failure("goal.invalid","Revise o título, o período e a forma de acompanhamento.");
  var now=DateTime.UtcNow;var goal=new UserGoal(Guid.NewGuid(),clientId,userId,null,title.Trim(),description?.Trim(),targetType,targetValue,0,startDate,endDate,"Active","#10B981",null,now,now,null);await goals.CreateAsync(goal,ct);return Result<UserGoal>.Success(goal);
 }
 public async Task<Result<UserGoal>> UpdateAsync(Guid id,Guid clientId,Guid userId,string title,string? description,string targetType,int targetValue,DateOnly startDate,DateOnly? endDate,CancellationToken ct=default){
  var current=await goals.GetAsync(id,clientId,userId,ct);if(current is null)return Result<UserGoal>.Failure("goal.not_found","Este objetivo não foi encontrado.");
  if(!IsValid(title,targetType,targetValue,startDate,endDate))return Result<UserGoal>.Failure("goal.invalid","Revise o título, o período e a forma de acompanhamento.");
  var updated=current with{Title=title.Trim(),Description=description?.Trim(),TargetType=targetType,TargetValue=targetValue,StartDate=startDate,EndDate=endDate,UpdatedAt=DateTime.UtcNow};
  await goals.UpdateAsync(updated,ct);return Result<UserGoal>.Success(updated);
 }
 public Task CompleteAsync(Guid id,Guid c,Guid u,CancellationToken ct=default)=>goals.SetStatusAsync(id,c,u,"Completed",ct);
 public Task PauseAsync(Guid id,Guid c,Guid u,CancellationToken ct=default)=>goals.SetStatusAsync(id,c,u,"Paused",ct);
 public Task ResumeAsync(Guid id,Guid c,Guid u,CancellationToken ct=default)=>goals.SetStatusAsync(id,c,u,"Active",ct);
 public Task CancelAsync(Guid id,Guid c,Guid u,CancellationToken ct=default)=>goals.SetStatusAsync(id,c,u,"Canceled",ct);
 static bool IsValid(string title,string targetType,int targetValue,DateOnly startDate,DateOnly? endDate)=>!string.IsNullOrWhiteSpace(title)&&title.Trim().Length<=160&&TargetTypes.Contains(targetType)&&targetValue>0&&(endDate is null||endDate>=startDate);
 public Task SetStatusAsync(Guid id,Guid c,Guid u,string status,CancellationToken ct=default)=>goals.SetStatusAsync(id,c,u,status,ct);
}
