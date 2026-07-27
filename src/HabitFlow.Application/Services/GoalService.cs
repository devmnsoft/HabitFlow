using HabitFlow.Domain;
using HabitFlow.Shared;
namespace HabitFlow.Application;
public sealed class GoalService(IUserGoalRepository goals,FeatureAccessService access)
{
 public Task<IReadOnlyList<UserGoal>> ListAsync(Guid clientId,Guid userId,CancellationToken ct=default)=>goals.ListAsync(clientId,userId,ct);
 public Task<UserGoal?> GetAsync(Guid id,Guid clientId,Guid userId,CancellationToken ct=default)=>goals.GetAsync(id,clientId,userId,ct);
 public async Task<Result<UserGoal>> CreateAsync(Guid clientId,Guid userId,string title,string? description,string targetType,int targetValue,DateOnly startDate,DateOnly? endDate,CancellationToken ct=default){
  var limit=await access.GetLimitAsync(userId,PlanFeatureCodes.ActiveGoalsLimit,ct); var active=await goals.CountActiveAsync(clientId,userId,ct);
  if(limit is >=0 && active>=limit)return Result<UserGoal>.Failure("goal.limit","Seu limite de objetivos ativos foi alcançado. Os objetivos que você já criou continuam aqui.");
  if(string.IsNullOrWhiteSpace(title)||targetValue<1)return Result<UserGoal>.Failure("goal.invalid","Revise o título e a meta do objetivo.");
  var now=DateTime.UtcNow;var goal=new UserGoal(Guid.NewGuid(),clientId,userId,null,title.Trim(),description?.Trim(),targetType,targetValue,0,startDate,endDate,"Active","#10B981",null,now,now,null);await goals.CreateAsync(goal,ct);return Result<UserGoal>.Success(goal);
 }
 public Task SetStatusAsync(Guid id,Guid c,Guid u,string status,CancellationToken ct=default)=>goals.SetStatusAsync(id,c,u,status,ct);
}
