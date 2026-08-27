using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HabitFlow.Web.Models;

namespace HabitFlow.Web.Controllers;

[Authorize]
[Route("my-day")]
public sealed class MyDayController(DailyRoutinePlannerService planner,HabitScheduleExceptionService schedule,DailyRoutineActionService actions,CompleteHabitUseCase complete,UndoHabitCompletionUseCase undo,UserTimeZoneService timeZone, ILogger<MyDayController> logger) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        try
        {
            var model = DailyRoutineViewModelMapper.From(await planner.BuildAsync(new(this.CurrentClientId(),this.CurrentUserId(),timeZone.Today()),ct));
            var greeting = timeZone.LocalNow().Hour switch { < 12 => "Bom dia", < 18 => "Boa tarde", _ => "Boa noite" };
            logger.LogInformation("daily_center.opened CorrelationId={CorrelationId}", HttpContext.TraceIdentifier);
            return View(model with { Greeting = greeting });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "daily_center.failed CorrelationId={CorrelationId}", HttpContext.TraceIdentifier);
            TempData["Error"] = "Não foi possível preparar o seu dia agora. Tente novamente em instantes.";
            return RedirectToAction("Index", "Dashboard");
        }
    }

    [HttpPost("{habitId:guid}/complete"),ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(Guid habitId,string idempotencyKey,CancellationToken ct)
    { var result=await complete.ExecuteAsync(new(this.CurrentClientId(),this.CurrentUserId(),habitId,timeZone.Today(),idempotencyKey,"MyDay",HttpContext.TraceIdentifier),ct); TempData[result.IsSuccess?"Success":"Error"]=result.IsSuccess?"Um passo concluído. Continue no seu ritmo.":result.Error.Message; return RedirectToAction(nameof(Index)); }

    [HttpPost("{habitId:guid}/undo"),ValidateAntiForgeryToken]
    public async Task<IActionResult> Undo(Guid habitId,string idempotencyKey,CancellationToken ct)
    { var result=await undo.ExecuteAsync(new(this.CurrentClientId(),this.CurrentUserId(),habitId,timeZone.Today(),idempotencyKey,"MyDay",HttpContext.TraceIdentifier),ct); TempData[result.IsSuccess?"Success":"Error"]=result.IsSuccess?"Conclusão desfeita.":result.Error.Message; return RedirectToAction(nameof(Index)); }

    [HttpPost("{habitId:guid}/move-tomorrow"),ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveTomorrow(Guid habitId,CancellationToken ct)
    { var today=timeZone.Today(); var result=await schedule.SetAsync(this.CurrentClientId(),this.CurrentUserId(),habitId,today,HabitScheduleExceptionType.Moved,today.AddDays(1),null,ct); TempData[result.IsSuccess?"Success":"Error"]=result.IsSuccess?"Movido para amanhã sem alterar sua rotina.":result.Error.Message; return RedirectToAction(nameof(Index)); }

    [HttpPost("{habitId:guid}/excuse"),ValidateAntiForgeryToken]
    public async Task<IActionResult> Excuse(Guid habitId,CancellationToken ct)
    { var result=await schedule.SetAsync(this.CurrentClientId(),this.CurrentUserId(),habitId,timeZone.Today(),HabitScheduleExceptionType.Excused,null,"Pausa somente hoje",ct); TempData[result.IsSuccess?"Success":"Error"]=result.IsSuccess?"Hoje ficou livre. Sua sequência está preservada.":result.Error.Message; return RedirectToAction(nameof(Index)); }

    [HttpPost("{habitId:guid}/restore"),ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(Guid habitId,int version,CancellationToken ct)
    { var result=await actions.RestoreAsync(this.CurrentClientId(),this.CurrentUserId(),habitId,timeZone.Today(),version,version,ct); TempData[result.Succeeded?"Success":"Error"]=result.Message; return RedirectToAction(nameof(Index)); }

    [HttpPost("{habitId:guid}/time"),ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeTime(Guid habitId,TimeOnly preferredTime,int version,CancellationToken ct)
    { var result=await actions.ChangeTimeAsync(this.CurrentClientId(),this.CurrentUserId(),habitId,timeZone.Today(),preferredTime,version,ct); TempData[result.Succeeded?"Success":"Error"]=result.Message; return RedirectToAction(nameof(Index)); }

    [HttpPost("{habitId:guid}/reorder"),ValidateAntiForgeryToken]
    public async Task<IActionResult> Reorder(Guid habitId,int sortOrder,int version,CancellationToken ct)
    { var result=await actions.ReorderAsync(this.CurrentClientId(),this.CurrentUserId(),habitId,timeZone.Today(),sortOrder,version,ct); TempData[result.Succeeded?"Success":"Error"]=result.Message; return RedirectToAction(nameof(Index)); }
}
