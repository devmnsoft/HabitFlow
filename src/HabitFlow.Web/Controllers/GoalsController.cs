using HabitFlow.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HabitFlow.Web.Models;

namespace HabitFlow.Web.Controllers;

[Authorize,Route("goals")]
public sealed class GoalsController(GoalService goals,GoalQueryService query,GoalLifecycleService lifecycle,GoalLinkedHabitService links,CurrentUserContext current):Controller
{
 [HttpGet("")] public async Task<IActionResult> Index(string? search,string? status,string? sort,CancellationToken ct)=>current.ClientId is Guid c?View(await query.ListAsync(c,current.UserId,search,status,sort,ct)):Forbid();
 [HttpGet("create")] public IActionResult Create()=>View(GoalFormViewModel.Create());
 [HttpPost("create"),ValidateAntiForgeryToken] public async Task<IActionResult> Create(GoalFormViewModel model,CancellationToken ct){if(current.ClientId is not Guid c)return Forbid();if(!ModelState.IsValid)return View(model);var result=await goals.CreateAsync(c,current.UserId,model.Title,model.Description,model.TargetType,model.TargetValue,model.StartDate,model.EndDate,ct);if(result.IsFailure){ModelState.AddModelError("",result.Error.Message);return View(model);}TempData["Success"]="Objetivo criado. Agora conecte os hábitos que ajudam nessa direção.";return RedirectToAction(nameof(Detail),new{id=result.Value!.Id});}
 [HttpGet("{id:guid}")] public async Task<IActionResult> Detail(Guid id,CancellationToken ct)=>current.ClientId is Guid c&&await query.GetAsync(id,c,current.UserId,ct) is {} goal?View(goal):NotFound();
 [HttpGet("{id:guid}/edit")] public async Task<IActionResult> Edit(Guid id,CancellationToken ct)=>current.ClientId is Guid c&&await goals.GetAsync(id,c,current.UserId,ct) is {} goal?View(GoalFormViewModel.From(goal)):NotFound();
 [HttpPost("{id:guid}/edit"),ValidateAntiForgeryToken] public async Task<IActionResult> Edit(Guid id,GoalFormViewModel model,CancellationToken ct){if(current.ClientId is not Guid c)return Forbid();if(!ModelState.IsValid){model=WithGoalId(model,id);return View(model);}var result=await goals.UpdateAsync(id,c,current.UserId,model.Title,model.Description,model.TargetType,model.TargetValue,model.StartDate,model.EndDate,ct);if(result.IsFailure){ModelState.AddModelError("",result.Error.Message);model=WithGoalId(model,id);return result.Error.Code=="goal.not_found"?NotFound():View(model);}TempData["Success"]="Objetivo atualizado.";return RedirectToAction(nameof(Detail),new{id});}
 [HttpPost("{id:guid}/complete"),ValidateAntiForgeryToken] public Task<IActionResult> Complete(Guid id,CancellationToken ct)=>Change(id,"Completed",ct);
 [HttpPost("{id:guid}/pause"),ValidateAntiForgeryToken] public Task<IActionResult> Pause(Guid id,CancellationToken ct)=>Change(id,"Paused",ct);
 [HttpPost("{id:guid}/resume"),ValidateAntiForgeryToken] public Task<IActionResult> Resume(Guid id,CancellationToken ct)=>Change(id,"Active",ct);
 [HttpPost("{id:guid}/cancel"),ValidateAntiForgeryToken] public Task<IActionResult> Cancel(Guid id,CancellationToken ct)=>Change(id,"Canceled",ct);
 [HttpPost("{id:guid}/link-habit"),ValidateAntiForgeryToken] public async Task<IActionResult> LinkHabit(Guid id,Guid habitId,CancellationToken ct)=>await ChangeLink(id,habitId,true,ct);
 [HttpPost("{id:guid}/unlink-habit"),ValidateAntiForgeryToken] public async Task<IActionResult> UnlinkHabit(Guid id,Guid habitId,CancellationToken ct)=>await ChangeLink(id,habitId,false,ct);
 async Task<IActionResult> Change(Guid id,string status,CancellationToken ct){if(current.ClientId is not Guid c)return Forbid();var result=await lifecycle.ChangeAsync(id,c,current.UserId,status,ct);if(result.IsFailure)TempData["Error"]=result.Error.Message;else TempData["Success"]="Objetivo atualizado.";return result.Error.Code=="goal.not_found"?NotFound():RedirectToAction(nameof(Detail),new{id});}
 async Task<IActionResult> ChangeLink(Guid id,Guid habitId,bool link,CancellationToken ct){if(current.ClientId is not Guid c)return Forbid();var result=link?await links.LinkAsync(id,habitId,c,current.UserId,ct):await links.UnlinkAsync(id,habitId,c,current.UserId,ct);TempData[result.IsSuccess?"Success":"Error"]=result.IsSuccess?(link?"Hábito conectado.":"Hábito desconectado."):result.Error.Message;return result.Error.Code=="goal.link.not_found"?NotFound():RedirectToAction(nameof(Detail),new{id});}

 static GoalFormViewModel WithGoalId(GoalFormViewModel model,Guid id)=>new(){GoalId=id,Title=model.Title,Description=model.Description,TargetType=model.TargetType,TargetValue=model.TargetValue,StartDate=model.StartDate,EndDate=model.EndDate};
}
