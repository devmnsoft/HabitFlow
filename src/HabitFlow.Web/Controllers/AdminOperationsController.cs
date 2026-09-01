using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;
[Authorize(Policy="RequireSuperAdmin")]
[Route("admin")]
public sealed class AdminOperationsController(OperationsCenterService service, AdminAuditService audit) : Controller
{
    [HttpGet("operations")]
    public async Task<IActionResult> Operations(CancellationToken ct) { await audit.LogAsync(this.CurrentUserSnapshot(),"operations.opened",null,null,null,ct); return View("~/Views/Admin/Operations.cshtml",await service.GetAsync(ct)); }
    [HttpGet("system-health")]
    public async Task<IActionResult> Health(CancellationToken ct) { var report=await service.HealthAsync(ct); await audit.LogAsync(this.CurrentUserSnapshot(),report.OverallStatus=="Operacional"?"system_health.checked":"system_health.failed",report.OverallStatus,null,null,ct); return View("~/Views/Admin/OperationsHealth.cshtml",report); }
    [HttpGet("logs")]
    public async Task<IActionResult> Logs([FromQuery]StructuredLogFilter filter,CancellationToken ct) { await audit.LogAsync(this.CurrentUserSnapshot(),"admin.logs_viewed",null,null,null,ct); return View("~/Views/Admin/OperationsLogs.cshtml",await service.LogsAsync(filter,ct)); }
    [HttpGet("logs/{id:guid}")] public async Task<IActionResult> Log(Guid id,CancellationToken ct) { var item=await service.LogAsync(id,ct); return item is null?NotFound():View("~/Views/Admin/OperationLog.cshtml",item); }
    [HttpGet("logs/export")] public async Task<IActionResult> Export([FromQuery]StructuredLogFilter filter,CancellationToken ct)=>File(await service.ExportAsync(this.CurrentUserSnapshot(),filter,ct),"text/csv; charset=utf-8",$"habitflow-logs-{DateTime.UtcNow:yyyyMMddHHmm}.csv");
    [HttpPost("operations/alerts/{id:guid}/resolve"),ValidateAntiForgeryToken] public async Task<IActionResult> Resolve(Guid id,CancellationToken ct) { await service.ResolveAsync(this.CurrentUserSnapshot(),id,ct); TempData["Success"]="Alerta resolvido e auditado."; return RedirectToAction(nameof(Operations)); }
}
