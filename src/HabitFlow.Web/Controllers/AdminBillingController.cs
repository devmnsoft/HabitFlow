using HabitFlow.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace HabitFlow.Web.Controllers;
[Authorize(Roles="Admin")]
public sealed class AdminBillingController(AdminMetricsService metrics) : Controller
{ [HttpGet("admin/leads")] public async Task<IActionResult> Leads(CancellationToken ct)=>View("~/Views/Admin/Leads.cshtml",await metrics.GetPremiumLeadsAsync(new(),ct)); [HttpGet("admin/finance")] public async Task<IActionResult> Finance(CancellationToken ct)=>View("~/Views/Admin/Finance.cshtml",await metrics.GetFinancialSummaryAsync(ct)); }
