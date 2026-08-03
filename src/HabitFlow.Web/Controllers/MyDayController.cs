using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize]
[Route("my-day")]
public sealed class MyDayController(DailyRoutinePlannerService planner,HabitScheduleExceptionService schedule,CompleteHabitUseCase complete,UndoHabitCompletionUseCase undo,UserTimeZoneService timeZone) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct) => View(await planner.BuildAsync(new(this.CurrentClientId(),this.CurrentUserId(),timeZone.Today()),ct));

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
}
