using HabitFlow.Application;
using HabitFlow.Domain;
using HabitFlow.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize,Route("challenges")]
public sealed class ChallengesController(UserChallengeService service,IHabitRepository habits) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var clientId=this.CurrentClientId(); var userId=this.CurrentUserId();
        return View(new ChallengePageViewModel(await service.ListAsync(clientId,userId,ct),await habits.ListActiveAsync(clientId,userId,ct)));
    }

    [HttpPost("start"),ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(Guid habitId,int durationDays,CancellationToken ct)
    {
        var result=await service.StartAsync(this.CurrentClientId(),this.CurrentUserId(),habitId,durationDays,HttpContext.TraceIdentifier,ct);
        TempData[result.IsSuccess?"Success":"Error"]=result.IsSuccess?"Desafio iniciado. Um dia de cada vez.":result.Error.Message;
        if (result.IsFailure && result.Error.Code=="challenge.plan_required") return Redirect("/plans?from=challenge");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:guid}/abandon"),ValidateAntiForgeryToken]
    public async Task<IActionResult> Abandon(Guid id,CancellationToken ct)
    { var result=await service.AbandonAsync(this.CurrentClientId(),this.CurrentUserId(),id,HttpContext.TraceIdentifier,ct); TempData[result.IsSuccess?"Success":"Error"]=result.IsSuccess?"Desafio encerrado. Seu progresso continua sendo seu.":result.Error.Message; return RedirectToAction(nameof(Index)); }
}
