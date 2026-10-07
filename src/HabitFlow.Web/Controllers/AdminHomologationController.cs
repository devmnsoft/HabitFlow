using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace HabitFlow.Web.Controllers;

[Authorize(Policy = "RequireAdmin")]
[Route("admin/homologation")]
public sealed class AdminHomologationController(ProductActivationService activation, CurrentUserContext currentUser) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(DateOnly? from, DateOnly? to, string? tenant, string? plan, string? status, CancellationToken ct)
    {
        if (!HomologationAccess.CanView(currentUser.Role.ToString()) || currentUser.IsSuperAdmin) return currentUser.IsSuperAdmin ? Redirect("/superadmin/homologation") : Forbid();
        if (!currentUser.ClientId.HasValue) return Forbid();
        var query = HomologationAccess.Scope(currentUser.Role.ToString(), currentUser.ClientId, new HomologationQuery(from, to, tenant, plan, status, currentUser.ClientId));
        return View("~/Views/Admin/Homologation.cshtml", await activation.ReportAsync(query, ct));
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(DateOnly? from, DateOnly? to, string? tenant, string? plan, string? status, CancellationToken ct)
    {
        if (!currentUser.ClientId.HasValue || currentUser.IsSuperAdmin) return Forbid();
        var query = HomologationAccess.Scope(currentUser.Role.ToString(), currentUser.ClientId, new HomologationQuery(from, to, tenant, plan, status, currentUser.ClientId));
        var report = await activation.ReportAsync(query, ct);
        return File(Encoding.UTF8.GetBytes(HomologationCsv.Write(report)), "text/csv", "habitflow-homologacao-tenant.csv");
    }
}
