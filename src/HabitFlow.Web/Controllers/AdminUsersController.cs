using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize(Roles = "Admin")]
[Route("admin/users")]
public sealed class AdminUsersController(AdminUserService users, ILogger<AdminUsersController> logger) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] AdminUserFilter filter, CancellationToken ct){try{return View("~/Views/Admin/Users.cshtml", await users.SearchUsersAsync(filter, ct));}catch(Exception ex){logger.LogError(ex,"Admin users");TempData["Error"]="Não foi possível listar usuários.";return View("~/Views/Admin/Users.cshtml");}}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id, CancellationToken ct){try{return View("~/Views/Admin/UserDetail.cshtml", await users.GetUserDetailAsync(id, ct));}catch(Exception ex){logger.LogError(ex,"Admin user detail");TempData["Error"]="Não foi possível carregar usuário.";return RedirectToAction(nameof(Index));}}
    [HttpPost("{id:guid}/status")][ValidateAntiForgeryToken]
    public async Task<IActionResult> Status(Guid id, AccountStatus status, string reason, CancellationToken ct){var r=await users.UpdateAccountStatusAsync(this.CurrentUserSnapshot(),id,status,reason,ct);TempData[r.IsSuccess?"Success":"Error"]=r.IsSuccess?"Status atualizado.":r.Error.Message;return RedirectToAction(nameof(Detail),new{id});}
    [HttpPost("{id:guid}/risk")][ValidateAntiForgeryToken]
    public async Task<IActionResult> Risk(Guid id, RiskStatus riskStatus, string reason, CancellationToken ct){var r=await users.UpdateRiskStatusAsync(this.CurrentUserSnapshot(),id,riskStatus,reason,ct);TempData[r.IsSuccess?"Success":"Error"]=r.IsSuccess?"Risco atualizado.":r.Error.Message;return RedirectToAction(nameof(Detail),new{id});}
    [HttpPost("{id:guid}/plan")][ValidateAntiForgeryToken]
    public async Task<IActionResult> Plan(Guid id, UserPlan plan, PlanStatus planStatus, string reason, CancellationToken ct){var r=await users.UpdatePlanAsync(this.CurrentUserSnapshot(),id,plan,planStatus,reason,ct);TempData[r.IsSuccess?"Success":"Error"]=r.IsSuccess?"Plano atualizado.":r.Error.Message;return RedirectToAction(nameof(Detail),new{id});}
    [HttpPost("{id:guid}/notes")][ValidateAntiForgeryToken]
    public async Task<IActionResult> Notes(Guid id, string note, CancellationToken ct){var r=await users.AddAdminNoteAsync(this.CurrentUserSnapshot(),id,note,ct);TempData[r.IsSuccess?"Success":"Error"]=r.IsSuccess?"Nota adicionada.":r.Error.Message;return RedirectToAction(nameof(Detail),new{id});}
}
