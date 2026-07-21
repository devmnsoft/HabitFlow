using HabitFlow.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace HabitFlow.Web.Controllers;
[Authorize(Roles="Admin")][Route("admin/logs")]
public sealed class AdminLogsController(AdminAuditQueryService service) : Controller
{ [HttpGet("system")] public async Task<IActionResult> System([FromQuery]AuditLogFilter filter,CancellationToken ct)=>View("~/Views/Admin/SystemLogs.cshtml",await service.SearchSystemLogsAsync(filter,ct)); [HttpGet("admin")] public async Task<IActionResult> Admin([FromQuery]AuditLogFilter filter,CancellationToken ct)=>View("~/Views/Admin/AdminLogs.cshtml",await service.SearchAdminLogsAsync(filter,ct)); [HttpPost("system/{id:guid}/read")][ValidateAntiForgeryToken] public async Task<IActionResult> Read(Guid id,CancellationToken ct){await service.MarkSystemLogAsReadAsync(this.CurrentUserSnapshot(),id,ct);return RedirectToAction(nameof(System));}}
