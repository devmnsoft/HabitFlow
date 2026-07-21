using HabitFlow.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize(Roles = "Admin")]
[Route("admin/exports")]
public sealed class AdminExportsController(AdminExportService exports) : Controller
{
    [HttpGet("users")] public async Task<IActionResult> Users(CancellationToken ct){var r=await exports.ExportUsersCsvAsync(this.CurrentUserSnapshot(),new(),ct);return File(r.Value ?? [],"text/csv","habitflow-admin-users.csv");}
    [HttpGet("leads")] public async Task<IActionResult> Leads(CancellationToken ct){var r=await exports.ExportPremiumLeadsCsvAsync(this.CurrentUserSnapshot(),new(),ct);return File(r.Value ?? [],"text/csv","habitflow-admin-leads.csv");}
    [HttpGet("support")] public async Task<IActionResult> Support(CancellationToken ct){var r=await exports.ExportSupportTicketsCsvAsync(this.CurrentUserSnapshot(),new(),ct);return File(r.Value ?? [],"text/csv","habitflow-admin-support.csv");}
    [HttpGet("lgpd")] public async Task<IActionResult> Lgpd(CancellationToken ct){var r=await exports.ExportLgpdRequestsCsvAsync(this.CurrentUserSnapshot(),new(),ct);return File(r.Value ?? [],"text/csv","habitflow-admin-lgpd.csv");}
    [HttpGet("system-logs")] public async Task<IActionResult> SystemLogs(CancellationToken ct){var r=await exports.ExportSystemLogsCsvAsync(this.CurrentUserSnapshot(),new(),ct);return File(r.Value ?? [],"text/csv","habitflow-system-logs.csv");}
    [HttpGet("admin-audit")] public async Task<IActionResult> AdminAudit(CancellationToken ct){var r=await exports.ExportAdminAuditCsvAsync(this.CurrentUserSnapshot(),new(),ct);return File(r.Value ?? [],"text/csv","habitflow-admin-audit.csv");}
}
