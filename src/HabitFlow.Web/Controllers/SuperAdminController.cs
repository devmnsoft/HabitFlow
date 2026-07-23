using HabitFlow.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace HabitFlow.Web.Controllers;

[Authorize(Roles = "SuperAdmin")]
[Route("superadmin")]
public sealed class SuperAdminController(SuperAdminService dashboard, ClientService clients, EntitlementService entitlements) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct) => View(await dashboard.GetDashboardAsync(ct));

    [HttpGet("clients")]
    public async Task<IActionResult> Clients([FromQuery] ClientFilter filter, CancellationToken ct)
    {
        var result = await clients.SearchAsync(filter, ct);
        return View("~/Views/SuperAdmin/Clients/Index.cshtml", result.Value ?? Array.Empty<ClientListItemDto>());
    }

    [HttpGet("clients/{id:guid}")]
    public async Task<IActionResult> ClientDetails(Guid id, CancellationToken ct)
    {
        var result = await clients.GetByIdAsync(id, ct);
        return result.IsFailure ? RedirectToAction(nameof(Clients)) : View("~/Views/SuperAdmin/Clients/Details.cshtml", result.Value);
    }

    [HttpPost("clients/{id:guid}/block-paid-benefits")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BlockPaidBenefits(Guid id, string reason, CancellationToken ct) { await entitlements.BlockPaidBenefitsAsync(id, reason, this.CurrentUserSnapshot(), ct); return RedirectToAction(nameof(ClientDetails), new { id }); }
    [HttpPost("clients/{id:guid}/release-paid-benefits")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReleasePaidBenefits(Guid id, string reason, CancellationToken ct) { await entitlements.ReleasePaidBenefitsAsync(id, reason, this.CurrentUserSnapshot(), ct); return RedirectToAction(nameof(ClientDetails), new { id }); }


    [HttpGet("clients/{id:guid}/activity")] public IActionResult ClientActivity(Guid id) => View("~/Views/SuperAdmin/Simple.cshtml", $"Atividade do cliente {id}");
    [HttpPost("clients/{id:guid}/change-plan")] [ValidateAntiForgeryToken] public IActionResult ChangePlan(Guid id, string planCode, string reason) => RedirectToAction(nameof(ClientDetails), new { id });
    [HttpPost("payments/{id:guid}/mark-as-paid")] [ValidateAntiForgeryToken] public IActionResult MarkPaymentAsPaid(Guid id, string reason) => RedirectToAction(nameof(Payments));
    [HttpPost("payments/{id:guid}/mark-as-overdue")] [ValidateAntiForgeryToken] public IActionResult MarkPaymentAsOverdue(Guid id, string reason) => RedirectToAction(nameof(Overdue));
    [HttpPost("subscriptions/{id:guid}/cancel")] [ValidateAntiForgeryToken] public IActionResult CancelSubscription(Guid id, string reason) => RedirectToAction(nameof(Subscriptions));
    [HttpPost("subscriptions/{id:guid}/reactivate")] [ValidateAntiForgeryToken] public IActionResult ReactivateSubscription(Guid id, string reason) => RedirectToAction(nameof(Subscriptions));
    [HttpGet("export/payments")] public IActionResult ExportPayments() => File(Encoding.UTF8.GetBytes("Pagamento,Status,Metodo\n"), "text/csv", "habitflow-pagamentos.csv");
    [HttpGet("export/overdue")] public IActionResult ExportOverdue() => File(Encoding.UTF8.GetBytes("Cliente,Vencimento,Status\n"), "text/csv", "habitflow-inadimplentes.csv");
    [HttpGet("export/subscriptions")] public IActionResult ExportSubscriptions() => File(Encoding.UTF8.GetBytes("Cliente,Plano,Status\n"), "text/csv", "habitflow-assinaturas.csv");

    [HttpGet("plans")] public IActionResult Plans() => View("~/Views/SuperAdmin/Simple.cshtml", "Planos");
    [HttpGet("subscriptions")] public IActionResult Subscriptions() => View("~/Views/SuperAdmin/Simple.cshtml", "Assinaturas");
    [HttpGet("billing")] public IActionResult Billing() => View("~/Views/SuperAdmin/Simple.cshtml", "Faturamento");
    [HttpGet("payments")] public IActionResult Payments() => View("~/Views/SuperAdmin/Simple.cshtml", "Pagamentos Pix/Boleto Mercado Pago");
    [HttpGet("overdue")] public IActionResult Overdue() => View("~/Views/SuperAdmin/Simple.cshtml", "Inadimplentes");
    [HttpGet("audit")] public IActionResult Audit() => View("~/Views/SuperAdmin/Simple.cshtml", "Auditoria SuperAdmin");
    [HttpGet("system")] public IActionResult System() => View("~/Views/SuperAdmin/Simple.cshtml", "Sistema");

    [HttpGet("export/clients")]
    public async Task<IActionResult> ExportClients(CancellationToken ct)
    {
        var result = await clients.SearchAsync(new ClientFilter { PageSize = 100 }, ct);
        var csv = new StringBuilder("Nome,Documento,Email,Plano,Status\n");
        foreach (var c in result.Value ?? Array.Empty<ClientListItemDto>()) csv.AppendLine(string.Join(',', Safe(c.Name), Safe(c.Document), Safe(c.Email), c.Plan, c.Status));
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "habitflow-clientes.csv");
    }
    private static string Safe(string? v) { v ??= ""; if (v.StartsWith('=') || v.StartsWith('+') || v.StartsWith('-') || v.StartsWith('@')) v = "'" + v; return '"' + v.Replace("\"", "\"\"") + '"'; }
}
