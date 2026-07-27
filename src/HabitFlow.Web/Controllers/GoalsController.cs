using HabitFlow.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace HabitFlow.Web.Controllers;
[Authorize]
[Route("goals")]
public sealed class GoalsController(GoalService goals,CurrentUserContext current):Controller
{
 [HttpGet("")] public async Task<IActionResult> Index(CancellationToken ct)=>current.ClientId is Guid c?View(await goals.ListAsync(c,current.UserId,ct)):Forbid();
 [HttpGet("create")] public IActionResult Create()=>View();
 [HttpPost("create"),ValidateAntiForgeryToken] public async Task<IActionResult> Create(string title,string? description,string targetType,int targetValue,DateOnly startDate,DateOnly? endDate,CancellationToken ct){if(current.ClientId is not Guid c)return Forbid();var result=await goals.CreateAsync(c,current.UserId,title,description,targetType,targetValue,startDate,endDate,ct);if(result.IsFailure){ModelState.AddModelError("",result.Error.Message);return View();}return RedirectToAction(nameof(Detail),new{id=result.Value!.Id});}
 [HttpGet("{id:guid}")] public async Task<IActionResult> Detail(Guid id,CancellationToken ct)=>current.ClientId is Guid c&&await goals.GetAsync(id,c,current.UserId,ct) is {} goal?View(goal):NotFound();
 [HttpGet("{id:guid}/edit")] public async Task<IActionResult> Edit(Guid id,CancellationToken ct)=>current.ClientId is Guid c&&await goals.GetAsync(id,c,current.UserId,ct) is {} goal?View(goal):NotFound();
 [HttpPost("{id:guid}/edit"),ValidateAntiForgeryToken] public IActionResult Edit(Guid id)=>RedirectToAction(nameof(Detail),new{id});
 [HttpPost("{id:guid}/pause"),ValidateAntiForgeryToken] public Task<IActionResult> Pause(Guid id,CancellationToken ct)=>Change(id,"Paused",ct);
 [HttpPost("{id:guid}/resume"),ValidateAntiForgeryToken] public Task<IActionResult> Resume(Guid id,CancellationToken ct)=>Change(id,"Active",ct);
 [HttpPost("{id:guid}/cancel"),ValidateAntiForgeryToken] public Task<IActionResult> Cancel(Guid id,CancellationToken ct)=>Change(id,"Canceled",ct);
 async Task<IActionResult> Change(Guid id,string status,CancellationToken ct){if(current.ClientId is not Guid c)return Forbid();if(await goals.GetAsync(id,c,current.UserId,ct) is null)return NotFound();await goals.SetStatusAsync(id,c,current.UserId,status,ct);return RedirectToAction(nameof(Detail),new{id});}
}
