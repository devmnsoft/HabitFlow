using HabitFlow.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace HabitFlow.Web.Controllers;

[Authorize(Roles = "SuperAdmin")]
[Route("superadmin")]
public sealed class SuperAdminController(SuperAdminService dashboard, ClientService clients, EntitlementService entitlements, ClientCommunicationService communications, CustomerHealthService health) : Controller
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
    public async Task<IActionResult> BlockPaidBenefits(Guid id, string reason, CancellationToken ct)
    {
        if (!HasRequiredReason(reason)) return RedirectWithReasonError(nameof(ClientDetails), new { id });
        var result = await entitlements.BlockPaidBenefitsAsync(id, reason, this.CurrentUserSnapshot(), ct);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess
            ? "Benefícios pagos bloqueados. O cliente segue com acesso Free."
            : result.Error.Message;
        return RedirectToAction(nameof(ClientDetails), new { id });
    }

    [HttpPost("clients/{id:guid}/release-paid-benefits")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReleasePaidBenefits(Guid id, string reason, CancellationToken ct)
    {
        if (!HasRequiredReason(reason)) return RedirectWithReasonError(nameof(ClientDetails), new { id });
        var result = await entitlements.ReleasePaidBenefitsAsync(id, reason, this.CurrentUserSnapshot(), ct);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess
            ? "Benefícios pagos reativados com segurança."
            : result.Error.Message;
        return RedirectToAction(nameof(ClientDetails), new { id });
    }


    [HttpGet("clients/{id:guid}/activity")] public IActionResult ClientActivity(Guid id) => View("~/Views/SuperAdmin/Simple.cshtml", $"Atividade do cliente {id}");
    [HttpPost("clients/{id:guid}/change-plan")]
    [ValidateAntiForgeryToken]
    public IActionResult ChangePlan(Guid id, string planCode, string reason) =>
        HasRequiredReason(reason) ? RedirectToAction(nameof(ClientDetails), new { id }) : RedirectWithReasonError(nameof(ClientDetails), new { id });

    [HttpPost("payments/{id:guid}/mark-as-paid")]
    [ValidateAntiForgeryToken]
    public IActionResult MarkPaymentAsPaid(Guid id, string reason) =>
        HasRequiredReason(reason) ? RedirectToAction(nameof(Payments)) : RedirectWithReasonError(nameof(Payments));

    [HttpPost("payments/{id:guid}/mark-as-overdue")]
    [ValidateAntiForgeryToken]
    public IActionResult MarkPaymentAsOverdue(Guid id, string reason) =>
        HasRequiredReason(reason) ? RedirectToAction(nameof(Overdue)) : RedirectWithReasonError(nameof(Overdue));

    [HttpPost("subscriptions/{id:guid}/cancel")]
    [ValidateAntiForgeryToken]
    public IActionResult CancelSubscription(Guid id, string reason) =>
        HasRequiredReason(reason) ? RedirectToAction(nameof(Subscriptions)) : RedirectWithReasonError(nameof(Subscriptions));

    [HttpPost("subscriptions/{id:guid}/reactivate")]
    [ValidateAntiForgeryToken]
    public IActionResult ReactivateSubscription(Guid id, string reason) =>
        HasRequiredReason(reason) ? RedirectToAction(nameof(Subscriptions)) : RedirectWithReasonError(nameof(Subscriptions));
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
    [HttpGet("communications")] public async Task<IActionResult> Communications(CancellationToken ct) => View("~/Views/SuperAdmin/Communications.cshtml", await communications.ListAllAsync(new Domain.ClientCommunicationFilter(), ct));
    [HttpGet("customer-success")] public IActionResult CustomerSuccess() => View("~/Views/SuperAdmin/CustomerSuccess.cshtml", health.Calculate(Guid.Empty, false, false, false, false, true, false, false, false, true));
    [HttpGet("support")] public IActionResult SupportOperations() => View("~/Views/SuperAdmin/Simple.cshtml", "Suporte operacional com SLA inicial");

    [HttpGet("export/clients")]
    public async Task<IActionResult> ExportClients(CancellationToken ct)
    {
        var result = await clients.SearchAsync(new ClientFilter { PageSize = 100 }, ct);
        var csv = new StringBuilder("Nome,Documento,Email,Plano,Status\n");
        foreach (var c in result.Value ?? Array.Empty<ClientListItemDto>()) csv.AppendLine(string.Join(',', Safe(c.Name), Safe(c.Document), Safe(c.Email), c.Plan, c.Status));
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "habitflow-clientes.csv");
    }
    private static string Safe(string? v) { v ??= ""; if (v.StartsWith('=') || v.StartsWith('+') || v.StartsWith('-') || v.StartsWith('@')) v = "'" + v; return '"' + v.Replace("\"", "\"\"") + '"'; }
    private static bool HasRequiredReason(string? reason) => !string.IsNullOrWhiteSpace(reason) && reason.Trim().Length >= 5;
    private IActionResult RedirectWithReasonError(string actionName, object? routeValues = null)
    {
        TempData["Error"] = "Informe um motivo obrigatório com pelo menos 5 caracteres para ações sensíveis.";
        return RedirectToAction(actionName, routeValues);
    }
}
