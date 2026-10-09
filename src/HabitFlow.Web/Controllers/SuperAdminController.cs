using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace HabitFlow.Web.Controllers;

[Authorize(Roles = "SuperAdmin")]
[Route("superadmin")]
public sealed class SuperAdminController(
    SuperAdminService dashboard,
    ClientService clients,
    EntitlementService entitlements,
    ClientCommunicationService communications,
    SuperAdminOperationalService operations,
    SchemaMigrationStatusService schema,
    IPlanCatalogRepository planCatalog,
    ProductActivationService activation,
    OperationalIncidentService incidentService,
    SupportCenterService supportService,
    TenantHealthEvaluationService healthService,
    OperationalAiService aiService,
    ILogger<SuperAdminController> logger) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct) => View(await dashboard.GetDashboardAsync(ct));

    [HttpGet("clients")]
    public async Task<IActionResult> Clients([FromQuery] ClientFilter filter, CancellationToken ct)
    {
        var result = await clients.SearchAsync(filter, ct);
        return View("~/Views/SuperAdmin/Clients/Index.cshtml", result.Value ?? Array.Empty<ClientListItemDto>());
    }

    [HttpGet("users")]
    public IActionResult Users() => View("~/Views/SuperAdmin/Simple.cshtml", "Usuários da plataforma");

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

    [HttpGet("clients/{id:guid}/activity")]
    public IActionResult ClientActivity(Guid id) => View("~/Views/SuperAdmin/Simple.cshtml", $"Atividade do cliente {id}");

    [HttpPost("clients/{id:guid}/change-plan")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePlan(Guid id, string planCode, string reason, CancellationToken ct)
    {
        if (!HasRequiredReason(reason)) return RedirectWithReasonError(nameof(ClientDetails), new { id });
        var result = await operations.ChangeClientPlanAsync(id, planCode, reason, User.Identity?.Name ?? "superadmin", ct);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? "Plano alterado com auditoria." : result.Error.Message;
        return RedirectToAction(nameof(ClientDetails), new { id });
    }

    [HttpPost("payments/{id:guid}/mark-as-paid")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkPaymentAsPaid(Guid id, string reason, CancellationToken ct)
    {
        if (!HasRequiredReason(reason)) return RedirectWithReasonError(nameof(Payments));
        var result = await operations.MarkInvoicePaidAsync(id, reason, User.Identity?.Name ?? "superadmin", ct);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? "Pagamento marcado como pago e benefícios liberados." : result.Error.Message;
        return RedirectToAction(nameof(Payments));
    }

    [HttpPost("payments/{id:guid}/mark-as-overdue")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkPaymentAsOverdue(Guid id, string reason, CancellationToken ct)
    {
        if (!HasRequiredReason(reason)) return RedirectWithReasonError(nameof(Overdue));
        var result = await operations.MarkInvoiceOverdueAsync(id, reason, User.Identity?.Name ?? "superadmin", ct);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? "Fatura marcada como vencida." : result.Error.Message;
        return RedirectToAction(nameof(Overdue));
    }

    [HttpPost("subscriptions/{id:guid}/cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelSubscription(Guid id, string reason, CancellationToken ct)
    {
        if (!HasRequiredReason(reason)) return RedirectWithReasonError(nameof(Subscriptions));
        var result = await operations.CancelSubscriptionAsync(id, reason, User.Identity?.Name ?? "superadmin", ct);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? "Assinatura cancelada com auditoria." : result.Error.Message;
        return RedirectToAction(nameof(Subscriptions));
    }

    [HttpPost("subscriptions/{id:guid}/reactivate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReactivateSubscription(Guid id, string reason, CancellationToken ct)
    {
        if (!HasRequiredReason(reason)) return RedirectWithReasonError(nameof(Subscriptions));
        var result = await operations.ReactivateSubscriptionAsync(id, reason, User.Identity?.Name ?? "superadmin", ct);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? "Assinatura reativada e benefícios recalculados." : result.Error.Message;
        return RedirectToAction(nameof(Subscriptions));
    }

    [HttpGet("export/payments")] public IActionResult ExportPayments() => File(Encoding.UTF8.GetBytes("Pagamento,Status,Metodo\n"), "text/csv", "habitflow-pagamentos.csv");
    [HttpGet("export/overdue")] public IActionResult ExportOverdue() => File(Encoding.UTF8.GetBytes("Cliente,Vencimento,Status\n"), "text/csv", "habitflow-inadimplentes.csv");
    [HttpGet("export/subscriptions")] public IActionResult ExportSubscriptions() => File(Encoding.UTF8.GetBytes("Cliente,Plano,Status\n"), "text/csv", "habitflow-assinaturas.csv");

    [HttpGet("registrations")]
    public async Task<IActionResult> Registrations(CancellationToken ct) => View("~/Views/SuperAdmin/Registrations/Index.cshtml", await operations.GetRegistrationQualityAsync(ct));

    [HttpGet("reports/registrations")]
    public async Task<IActionResult> RegistrationReport(CancellationToken ct) => View("~/Views/SuperAdmin/Registrations/Index.cshtml", await operations.GetRegistrationQualityAsync(ct));

    [HttpGet("export/registrations")]
    public async Task<IActionResult> ExportRegistrations(CancellationToken ct)
    {
        var report = await operations.GetRegistrationQualityAsync(ct);
        var sb = new StringBuilder("Data;ClienteId;Tipo;Nome;Documento;Email;Plano;Beneficios;Pagamento;Admin principal;Usuarios vinculados;Onboarding concluido\n");
        foreach (var r in report.Recent) sb.AppendLine(string.Join(';', new[] { r.CreatedAt.ToString("O"), r.ClientId.ToString(), SafeRegistration(r.PersonType), SafeRegistration(r.Name), SafeRegistration(r.Document), SafeRegistration(r.Email), SafeRegistration(r.Plan), SafeRegistration(r.BenefitsStatus), SafeRegistration(r.PaymentStatus), SafeRegistration(r.AdminEmail), string.Empty, string.Empty }));
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "habitflow-registrations.csv");
    }

    private static string SafeRegistration(string? value)
    {
        var v = (value ?? string.Empty).Replace(";", ",").Replace("\r", " ").Replace("\n", " ");
        return v.Length > 0 && "=+-@".Contains(v[0]) ? "'" + v : v;
    }

    [HttpGet("plans")] public async Task<IActionResult> Plans(CancellationToken ct) => View("~/Views/SuperAdmin/Plans/Index.cshtml", await operations.ListPlansAsync(ct));
    [HttpGet("subscriptions")] public async Task<IActionResult> Subscriptions(CancellationToken ct) => View("~/Views/SuperAdmin/Subscriptions/Index.cshtml", await operations.ListSubscriptionsAsync(ct));
    [HttpGet("billing")] public async Task<IActionResult> Billing(CancellationToken ct) => View("~/Views/SuperAdmin/Payments/Index.cshtml", await operations.ListPaymentsAsync(null, ct));
    [HttpGet("payments")] public async Task<IActionResult> Payments(CancellationToken ct) => View("~/Views/SuperAdmin/Payments/Index.cshtml", await operations.ListPaymentsAsync(null, ct));
    [HttpGet("overdue")] public async Task<IActionResult> Overdue(CancellationToken ct) => View("~/Views/SuperAdmin/Overdue/Index.cshtml", await operations.ListPaymentsAsync("Overdue", ct));
    [HttpGet("audit")] public async Task<IActionResult> Audit(CancellationToken ct) => View("~/Views/SuperAdmin/Audit/Index.cshtml", await operations.ListAuditAsync(ct));
    [HttpGet("system")] public async Task<IActionResult> System(CancellationToken ct) => View("~/Views/SuperAdmin/SystemHealth/Index.cshtml", await schema.BuildStatusAsync(ct));
    [HttpGet("system-health")] public async Task<IActionResult> SystemHealth(CancellationToken ct) => View("~/Views/SuperAdmin/SystemHealth/Index.cshtml", await schema.BuildStatusAsync(ct));
    
    [HttpGet("system-health/plan-access")]
    public async Task<IActionResult> PlanAccessHealth(CancellationToken ct)
    {
        var plans = await planCatalog.GetPublicCatalogAsync(ct);
        var known = new[] { PlanCodes.Free, PlanCodes.Ritmo, PlanCodes.Evolucao, PlanCodes.Team, PlanCodes.Enterprise };
        return Ok(new
        {
            status = plans.Count > 0 ? "Healthy" : "Degraded",
            databaseRead = true,
            postgresCivilDateType = "date -> DateOnly?",
            knownPlanCodes = known,
            publishedPlans = plans.Select(plan => new { plan.Code, featureCount = plan.Features.Count }),
            generatedAtUtc = DateTime.UtcNow
        });
    }

    [HttpGet("communications")]
    public async Task<IActionResult> Communications(CancellationToken ct) =>
        View("~/Views/SuperAdmin/Communications.cshtml", await communications.ListAllAsync(new ClientCommunicationFilter(), ct));

    // ==========================================
    // 4. CUSTOMER SUCCESS E SAÚDE DO CLIENTE
    // ==========================================
    [HttpGet("customer-success")]
    public async Task<IActionResult> CustomerSuccess([FromQuery] string? health, [FromQuery] string? plan, [FromQuery] string? search, CancellationToken ct)
    {
        try
        {
            var query = new HomologationQuery(null, null, search, plan, null, null);
            var indicators = await healthService.EvaluateAllAsync(query, ct);

            if (!string.IsNullOrWhiteSpace(health))
            {
                indicators = indicators.Where(i => i.HealthCategory.Equals(health, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            ViewData["SelectedHealth"] = health;
            ViewData["SelectedPlan"] = plan;
            ViewData["Search"] = search;

            return View("~/Views/SuperAdmin/CustomerSuccess.cshtml", indicators);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Painel de Customer Success indisponível");
            return View("~/Views/SuperAdmin/CustomerSuccess.cshtml", Array.Empty<TenantCustomerSuccessIndicator>());
        }
    }

    [HttpPost("customer-success/ai-churn-analysis")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AnalyzeChurnRiskAi([FromForm] string clientName, [FromForm] int score, [FromForm] string riskFactors, CancellationToken ct)
    {
        var factors = (riskFactors ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var result = await aiService.ExplainChurnRiskAsync(clientName, score, factors, ct);
        return Json(new { success = true, suggestion = result.Suggestion, provider = result.Provider });
    }

    // ==========================================
    // 6. CENTRAL DE SUPORTE OPERACIONAL
    // ==========================================
    [HttpGet("support")]
    public async Task<IActionResult> SupportOperations([FromQuery] string? status, [FromQuery] string? priority, [FromQuery] string? category, [FromQuery] string? search, CancellationToken ct)
    {
        var tickets = await supportService.ListGlobalAsync(status, priority, category, search, ct);
        ViewData["SelectedStatus"] = status;
        ViewData["SelectedPriority"] = priority;
        ViewData["SelectedCategory"] = category;
        ViewData["Search"] = search;
        return View("~/Views/SuperAdmin/Support/Index.cshtml", tickets);
    }

    [HttpGet("support/{id:guid}")]
    public async Task<IActionResult> SupportTicketDetail(Guid id, CancellationToken ct)
    {
        var ticket = await supportService.GetGlobalAsync(id, ct);
        if (ticket is null)
        {
            TempData["Error"] = "Chamado não encontrado.";
            return RedirectToAction(nameof(SupportOperations));
        }

        var messages = await supportService.MessagesAsync(ticket.ClientId, ticket.Id, staff: true, ct);
        return View("~/Views/SuperAdmin/Support/Details.cshtml", (Ticket: ticket, Messages: messages));
    }

    [HttpPost("support/{id:guid}/reply")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SupportReply(Guid id, [FromForm] string message, [FromForm] bool isInternal, CancellationToken ct)
    {
        var ticket = await supportService.GetGlobalAsync(id, ct);
        if (ticket is null) return RedirectToAction(nameof(SupportOperations));

        var ok = await supportService.ReplyAsync(ticket.ClientId, this.CurrentUserId(), id, admin: true, message, isInternal, ct);
        TempData[ok ? "Success" : "Error"] = ok
            ? (isInternal ? "Nota interna registrada para a equipe operacional." : "Resposta enviada ao cliente com sucesso.")
            : "Não foi possível registrar a mensagem.";

        return RedirectToAction(nameof(SupportTicketDetail), new { id });
    }

    [HttpPost("support/{id:guid}/resolve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SupportResolve(Guid id, [FromForm] string resolutionReason, CancellationToken ct)
    {
        var ticket = await supportService.GetGlobalAsync(id, ct);
        if (ticket is null) return RedirectToAction(nameof(SupportOperations));

        var result = await supportService.ResolveAsync(ticket.ClientId, this.CurrentUserId(), id, resolutionReason, admin: true, ct);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess
            ? "Chamado marcado como resolvido com motivo registrado."
            : result.Error.Message;

        return RedirectToAction(nameof(SupportTicketDetail), new { id });
    }

    [HttpPost("support/ai-suggest")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SuggestTicketReplyAi([FromForm] string subject, [FromForm] string description, CancellationToken ct)
    {
        var suggestion = await aiService.SuggestTicketReplyAsync(subject, description, null, ct);
        return Json(new { success = true, suggestion = suggestion.Suggestion, provider = suggestion.Provider });
    }

    // ==========================================
    // 7. INCIDENTES E STATUS OPERACIONAL
    // ==========================================
    [HttpGet("incidents")]
    public async Task<IActionResult> Incidents([FromQuery] string? status, [FromQuery] string? severity, [FromQuery] string? search, CancellationToken ct)
    {
        var filter = new OperationalIncidentFilter(status, severity, search);
        var list = await incidentService.ListAsync(filter, ct);

        ViewData["SelectedStatus"] = status;
        ViewData["SelectedSeverity"] = severity;
        ViewData["Search"] = search;

        return View("~/Views/SuperAdmin/Incidents/Index.cshtml", list);
    }

    [HttpPost("incidents")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateIncident(
        [FromForm] string title,
        [FromForm] string description,
        [FromForm] string severity,
        [FromForm] string impact,
        [FromForm] int affectedTenantsCount,
        [FromForm] DateTime? estimatedResolutionAt,
        [FromForm] string? communicationNotes,
        CancellationToken ct)
    {
        var correlationId = HttpContext.TraceIdentifier;
        var email = User.Identity?.Name ?? "superadmin@habitflow.com";
        var userId = this.CurrentUserId().ToString();

        var result = await incidentService.CreateAsync(
            title, description, severity, impact, affectedTenantsCount,
            estimatedResolutionAt, userId, email, false, communicationNotes,
            email, correlationId, ct);

        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess
            ? "Incidente registrado na operação com auditoria."
            : result.Error.Message;

        return RedirectToAction(nameof(Incidents));
    }

    [HttpPost("incidents/{id:guid}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateIncidentStatus(
        Guid id,
        [FromForm] string newStatus,
        [FromForm] string? resolutionNotes,
        CancellationToken ct)
    {
        var correlationId = HttpContext.TraceIdentifier;
        var email = User.Identity?.Name ?? "superadmin@habitflow.com";
        var userId = this.CurrentUserId().ToString();

        var result = await incidentService.UpdateStatusAsync(id, newStatus, resolutionNotes, userId, email, correlationId, ct);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess
            ? $"Status do incidente atualizado para {newStatus}."
            : result.Error.Message;

        return RedirectToAction(nameof(Incidents));
    }

    [HttpPost("incidents/ai-explain")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExplainIncidentAi([FromForm] string title, [FromForm] string impact, [FromForm] string severity, CancellationToken ct)
    {
        var suggestion = await aiService.ExplainIncidentAsync(title, impact, severity, ct);
        return Json(new { success = true, suggestion = suggestion.Suggestion, provider = suggestion.Provider });
    }

    [HttpGet("homologation")]
    public async Task<IActionResult> Homologation(DateOnly? from, DateOnly? to, string? tenant, string? plan, string? status, CancellationToken ct)
    {
        var query = HomologationAccess.Scope("SuperAdmin", null, new HomologationQuery(from, to, tenant, plan, status, null));
        return View("~/Views/SuperAdmin/Homologation.cshtml", await activation.ReportAsync(query, ct));
    }

    [HttpGet("homologation/export")]
    public async Task<IActionResult> HomologationCsvExport(DateOnly? from, DateOnly? to, string? tenant, string? plan, string? status, CancellationToken ct)
    {
        var query = HomologationAccess.Scope("SuperAdmin", null, new HomologationQuery(from, to, tenant, plan, status, null));
        var report = await activation.ReportAsync(query, ct);
        return File(Encoding.UTF8.GetBytes(HomologationCsv.Write(report)), "text/csv", "habitflow-homologacao.csv");
    }

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
