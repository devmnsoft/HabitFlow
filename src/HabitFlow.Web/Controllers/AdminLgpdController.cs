using HabitFlow.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace HabitFlow.Web.Controllers;
[Authorize(Roles="Admin")][Route("admin/lgpd")]
public sealed class AdminLgpdController(AdminLgpdService service) : Controller
{ [HttpGet("")] public async Task<IActionResult> Index([FromQuery]LgpdRequestFilter filter,CancellationToken ct)=>View("~/Views/Admin/Lgpd.cshtml",await service.SearchRequestsAsync(filter,ct)); [HttpPost("{id:guid}/status")][ValidateAntiForgeryToken] public async Task<IActionResult> Status(Guid id,string status,string notes,CancellationToken ct){await service.UpdateRequestStatusAsync(this.CurrentUserSnapshot(),id,status,notes,ct);return RedirectToAction(nameof(Index));}}
