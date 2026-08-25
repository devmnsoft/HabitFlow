using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

public sealed record WeeklyGoalsPage(IReadOnlyList<WeeklyGoal> Goals, IReadOnlyList<Habit> Habits);

[Authorize]
public sealed class GamificationController(GamificationService gamification, ProgressSnapshotService progress,
    IHabitRepository habits, CurrentUserContext current) : Controller
{
    [HttpGet("/progress")]
    public async Task<IActionResult> Progress(CancellationToken ct)
    {
        if (current.ClientId is not Guid clientId) return Forbid();
        var daily = await progress.BuildDashboardAsync(clientId,current.UserId,ct);
        return View("~/Views/Gamification/Progress.cshtml",await gamification.SnapshotAsync(clientId,current.UserId,daily.CurrentStreak,daily.BestStreak,ct));
    }
    [HttpGet("/achievements")]
    public async Task<IActionResult> Achievements(CancellationToken ct)
    {
        if(current.ClientId is not Guid clientId)return Forbid();
        var daily=await progress.BuildDashboardAsync(clientId,current.UserId,ct);
        return View("~/Views/Gamification/Achievements.cshtml",await gamification.SnapshotAsync(clientId,current.UserId,daily.CurrentStreak,daily.BestStreak,ct));
    }
    [HttpGet("/weekly-goals")]
    public async Task<IActionResult> WeeklyGoals(CancellationToken ct)
    {
        if(current.ClientId is not Guid clientId)return Forbid();
        return View("~/Views/Gamification/WeeklyGoals.cshtml",new WeeklyGoalsPage(await gamification.GoalsAsync(clientId,current.UserId,ct),await habits.ListActiveAsync(clientId,current.UserId,ct)));
    }
    [HttpPost("/weekly-goals"),ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateWeeklyGoal(string name,int targetCompletions,List<Guid> habitIds,CancellationToken ct)
    {
        if(current.ClientId is not Guid clientId)return Forbid();
        var result=await gamification.CreateWeeklyGoalAsync(clientId,current.UserId,name,targetCompletions,habitIds,ct);
        TempData[result.IsSuccess?"Success":"Error"]=result.IsSuccess?"Meta criada. Cada passo desta semana conta.":result.Error.Message;
        return Redirect("/weekly-goals");
    }
    [HttpPost("/streak-freeze"),ValidateAntiForgeryToken]
    public async Task<IActionResult> Freeze(Guid habitId,DateOnly date,string? reason,CancellationToken ct)
    {
        if(current.ClientId is not Guid clientId)return Forbid();
        var result=await gamification.UseFreezeAsync(clientId,current.UserId,habitId,date,reason,ct);
        if(result.IsFailure && result.Error.Code=="plan.feature_locked")return Redirect("/plans?feature=streak_freeze");
        TempData[result.IsSuccess?"Success":"Error"]=result.IsSuccess?"Sequência protegida. O relatório continuará mostrando que não houve conclusão.":result.Error.Message;
        return Redirect("/progress");
    }
}
