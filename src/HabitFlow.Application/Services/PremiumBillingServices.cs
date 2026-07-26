using System.Text.Json;
using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed class PaymentMetadataSanitizer
{
    private static readonly string[] Sensitive = ["card", "token", "access_token", "password", "document", "cpf", "private_key", "authorization"];
    public string Sanitize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return "{}";
        using var doc = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(SanitizeElement(doc.RootElement));
    }
    private static object? SanitizeElement(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => Sensitive.Any(s => p.Name.Contains(s, StringComparison.OrdinalIgnoreCase)) ? "[REDACTED]" : SanitizeElement(p.Value)),
        JsonValueKind.Array => element.EnumerateArray().Select(SanitizeElement).ToArray(),
        JsonValueKind.String => element.GetString(), JsonValueKind.Number => element.GetRawText(), JsonValueKind.True => true, JsonValueKind.False => false, _ => null
    };
}

public sealed class PlanService(IPlanRepository plans, ISubscriptionRepository subscriptions, ILogger<PlanService> logger)
{
    public async Task<Result<IReadOnlyList<Plan>>> GetPublicPlansAsync(CancellationToken ct = default) { try { return Result<IReadOnlyList<Plan>>.Success(await plans.GetPublicPlansAsync(ct)); } catch (Exception ex) { logger.LogError(ex, "Erro ao buscar planos públicos"); return Result<IReadOnlyList<Plan>>.Failure("plans.error", "Não foi possível carregar os planos."); } }
    public async Task<Result<Plan>> GetPlanByCodeAsync(string code, CancellationToken ct = default) { try { var plan = await plans.GetByCodeAsync(code, ct); return plan is null ? Result<Plan>.Failure("plans.not_found", "Plano inválido.") : Result<Plan>.Success(plan); } catch (Exception ex) { logger.LogError(ex, "Erro ao buscar plano {PlanCode}", code); return Result<Plan>.Failure("plans.error", "Não foi possível carregar o plano."); } }
    public async Task<Result<Plan>> GetUserEffectivePlanAsync(Guid userId, CancellationToken ct = default) { var sub = await subscriptions.GetActiveOrLatestByUserIdAsync(userId, ct); var code = sub is { Status: SubscriptionStatus.Active or SubscriptionStatus.Trial or SubscriptionStatus.PastDue } ? sub.PlanCode : "free"; return await GetPlanByCodeAsync(code, ct); }
}

public sealed class PremiumAccessService(PlanService plans, ISubscriptionRepository subscriptions, IConfiguration config, ILogger<PremiumAccessService> logger)
{
    public async Task<Result<bool>> IsPremiumAsync(Guid userId, CancellationToken ct = default) { try { var s = await subscriptions.GetActiveOrLatestByUserIdAsync(userId, ct); var grace = config.GetValue<int?>("Payment:PastDueGraceDays") ?? 3; var premium = s is { PlanCode: not "free", Status: SubscriptionStatus.Active or SubscriptionStatus.Trial } || s is { PlanCode: not "free", Status: SubscriptionStatus.PastDue, CurrentPeriodEnd: not null } && s.CurrentPeriodEnd.Value.AddDays(grace) >= DateTime.UtcNow; return Result<bool>.Success(premium); } catch (Exception ex) { logger.LogError(ex, "Erro ao validar premium"); return Result<bool>.Failure("premium.error", "Não foi possível validar acesso premium."); } }
    public async Task<Result<int?>> GetHabitLimitAsync(Guid userId, CancellationToken ct = default) => (await IsPremiumAsync(userId, ct)).Value == true ? Result<int?>.Success(null) : Result<int?>.Success(AppConstants.FreePlanHabitLimit);
    public async Task<Result<bool>> CanCreateHabitAsync(Guid userId, int activeHabitsCount, CancellationToken ct = default) { var limit = await GetHabitLimitAsync(userId, ct); if (limit.IsFailure) return Result<bool>.Failure(limit.Error.Code, limit.Error.Message); return Result<bool>.Success(limit.Value is null || activeHabitsCount < limit.Value); }
}

public sealed class SubscriptionService(ISubscriptionRepository repo, IUserRepository users, IPaymentAuditRepository audit, ILogger<SubscriptionService> logger)
{
    public Task<Subscription?> GetUserSubscriptionAsync(Guid userId, CancellationToken ct = default) => repo.GetActiveOrLatestByUserIdAsync(userId, ct);
    public async Task<Result<Subscription>> CreatePendingSubscriptionAsync(Guid userId, string planCode, BillingCycle cycle, PaymentProvider provider, CancellationToken ct = default) { try { var now = DateTime.UtcNow; var s = new Subscription(Guid.NewGuid(), userId, planCode, SubscriptionStatus.Pending, cycle, provider, null, null, null, null, null, null, null, null, now, now); await repo.CreateAsync(s, ct); await audit.CreateAsync(new(Guid.NewGuid(), userId, s.Id, "subscription.pending", "Assinatura pendente criada.", "Info", null, now), ct); return Result<Subscription>.Success(s); } catch (Exception ex) { logger.LogError(ex, "Erro ao criar assinatura pendente"); return Result<Subscription>.Failure("subscription.create_error", "Não foi possível criar assinatura."); } }
    public async Task<Result> ActivateSubscriptionAsync(Guid id, string? providerPaymentId, CancellationToken ct = default) { try { var s = await repo.GetByIdAsync(id, ct); if (s is null) return Result.Failure("subscription.not_found", "Assinatura não encontrada."); var now = DateTime.UtcNow; var end = s.BillingCycle == BillingCycle.Yearly ? now.AddYears(1) : now.AddMonths(1); var active = s with { Status = SubscriptionStatus.Active, ProviderPaymentId = providerPaymentId, CurrentPeriodStart = now, CurrentPeriodEnd = end, UpdatedAt = now }; await repo.UpdateAsync(active, ct); var u = await users.GetByIdAsync(s.UserId, ct); if (u is not null) await users.UpdateAsync(u with { Plan = UserPlan.Premium, PlanStatus = PlanStatus.Active, UpdatedAt = now }, ct); await audit.CreateAsync(new(Guid.NewGuid(), s.UserId, s.Id, "subscription.activated", "Premium ativado por confirmação backend/webhook.", "Info", null, now), ct); return Result.Success(); } catch (Exception ex) { logger.LogError(ex, "Erro ao ativar assinatura {SubscriptionId}", id); return Result.Failure("subscription.activate_error", "Não foi possível ativar assinatura."); } }
    public async Task<Result> CancelSubscriptionAsync(Guid id, string reason, CancellationToken ct = default) { if (string.IsNullOrWhiteSpace(reason)) return Result.Failure("subscription.reason_required", "Motivo obrigatório."); var s = await repo.GetByIdAsync(id, ct); if (s is null) return Result.Failure("subscription.not_found", "Assinatura não encontrada."); var now = DateTime.UtcNow; await repo.UpdateAsync(s with { Status = SubscriptionStatus.Canceled, CanceledAt = now, UpdatedAt = now }, ct); var u = await users.GetByIdAsync(s.UserId, ct); if (u is not null) await users.UpdateAsync(u with { Plan = UserPlan.Free, PlanStatus = PlanStatus.Canceled, UpdatedAt = now }, ct); await audit.CreateAsync(new(Guid.NewGuid(), s.UserId, s.Id, "subscription.canceled", reason, "Warning", null, now), ct); return Result.Success(); }
    public async Task<Result> MarkPastDueAsync(Guid id, string reason, CancellationToken ct = default) { if (string.IsNullOrWhiteSpace(reason)) return Result.Failure("subscription.reason_required", "Motivo obrigatório."); var s = await repo.GetByIdAsync(id, ct); if (s is null) return Result.Failure("subscription.not_found", "Assinatura não encontrada."); await repo.UpdateAsync(s with { Status = SubscriptionStatus.PastDue, UpdatedAt = DateTime.UtcNow }, ct); return Result.Success(); }
    public async Task<Result> SyncUserPlanAsync(Guid userId, CancellationToken ct = default) { var s = await repo.GetActiveOrLatestByUserIdAsync(userId, ct); var u = await users.GetByIdAsync(userId, ct); if (u is null) return Result.Failure("user.not_found", "Usuário não encontrado."); var premium = s is { Status: SubscriptionStatus.Active or SubscriptionStatus.Trial or SubscriptionStatus.PastDue, PlanCode: not "free" }; await users.UpdateAsync(u with { Plan = premium ? UserPlan.Premium : UserPlan.Free, PlanStatus = premium ? PlanStatus.Active : PlanStatus.Inactive, UpdatedAt = DateTime.UtcNow }, ct); return Result.Success(); }
}

public sealed class PaymentAuditService(IPaymentAuditRepository repo, ILogger<PaymentAuditService> logger)
{ public async Task<Result> LogAsync(Guid? userId, Guid? subscriptionId, string action, string message, string severity = "Info", string? metadata = null, CancellationToken ct = default) { try { await repo.CreateAsync(new(Guid.NewGuid(), userId, subscriptionId, action, message, severity, metadata, DateTime.UtcNow), ct); return Result.Success(); } catch (Exception ex) { logger.LogError(ex, "Erro ao registrar auditoria de pagamento"); return Result.Failure("payment_audit.error", "Não foi possível auditar pagamento."); } } }

public sealed class PaymentCheckoutService(IPlanRepository plans, SubscriptionService subscriptions, IPaymentProviderService provider, IPaymentAuditRepository audit, ILogger<PaymentCheckoutService> logger)
{
    public async Task<Result<CheckoutPreference>> StartCheckoutAsync(Guid userId, string email, string name, string planCode, BillingCycle cycle, CancellationToken ct = default)
    {
        try
        {
            var validation = await ValidateCheckoutRequest(planCode, cycle, ct); if (validation.IsFailure) return Result<CheckoutPreference>.Failure(validation.Error.Code, validation.Error.Message);
            var plan = (await plans.GetByCodeAsync(planCode, ct))!; var subResult = await subscriptions.CreatePendingSubscriptionAsync(userId, planCode, cycle, PaymentProvider.MercadoPago, ct); if (subResult.IsFailure) return Result<CheckoutPreference>.Failure(subResult.Error.Code, subResult.Error.Message);
            var checkout = await provider.CreateCheckoutPreferenceAsync(new(userId, email, name, planCode, cycle), subResult.Value!, plan, ct); if (checkout.IsFailure) return checkout;
            await audit.CreateAsync(new(Guid.NewGuid(), userId, subResult.Value!.Id, "checkout.started", "Checkout iniciado.", "Info", null, DateTime.UtcNow), ct); return checkout;
        }
        catch (Exception ex) { logger.LogError(ex, "Erro ao iniciar checkout"); return Result<CheckoutPreference>.Failure("checkout.error", "Não foi possível iniciar checkout."); }
    }
    public async Task<Result> ValidateCheckoutRequest(string planCode, BillingCycle cycle, CancellationToken ct = default)
    { if (planCode is not (PlanCodes.Ritmo or PlanCodes.Evolucao)) return Result.Failure("checkout.invalid_plan", "Plano inválido para pagamento."); var p = await plans.GetByCodeAsync(planCode, ct); return p is null || !p.IsActive || !p.IsPublic ? Result.Failure("checkout.invalid_plan", "Plano indisponível.") : Result.Success(); }
}

public sealed class PaymentWebhookService(IPaymentWebhookRepository webhooks, ISubscriptionRepository subscriptions, IPaymentTransactionRepository transactions, SubscriptionService subscriptionService, PaymentMetadataSanitizer sanitizer, IPaymentProviderService provider, ILogger<PaymentWebhookService> logger)
{
    public async Task<Result> ReceiveAsync(PaymentProvider paymentProvider, string payload, IReadOnlyDictionary<string,string> headers, CancellationToken ct = default)
    {
        var safe = sanitizer.Sanitize(payload); var webhook = new PaymentWebhookEvent(Guid.NewGuid(), paymentProvider, null, null, "Received", DateTime.UtcNow, null, null, null, null, safe, null); await webhooks.CreateAsync(webhook, ct);
        try { var valid = await provider.ValidateWebhookAsync(payload, headers, ct); if (valid.IsFailure) { await webhooks.MarkProcessedAsync(webhook.Id, null, null, null, valid.Error.Message, ct); return valid; } return await ProcessMercadoPagoEventAsync(webhook.Id, payload, ct); }
        catch (Exception ex) { logger.LogError(ex, "Erro em webhook de pagamento"); await webhooks.MarkProcessedAsync(webhook.Id, null, null, null, "Erro ao processar webhook", ct); return Result.Failure("webhook.error", "Webhook recebido com erro de processamento."); }
    }
    public async Task<Result> ProcessMercadoPagoEventAsync(Guid webhookEventId, string payload, CancellationToken ct = default)
    {
        using var doc = JsonDocument.Parse(payload); var root = doc.RootElement; var paymentId = TryGet(root, "data", "id") ?? TryGet(root, "id"); if (string.IsNullOrWhiteSpace(paymentId)) { await webhooks.MarkProcessedAsync(webhookEventId, null, null, null, "payment id ausente", ct); return Result.Failure("webhook.payment_id_missing", "Identificador do pagamento ausente."); }
        var payment = await provider.GetPaymentAsync(paymentId, ct); if (payment.IsFailure) return Result.Failure(payment.Error.Code, payment.Error.Message); return await UpdateSubscriptionFromPaymentAsync(webhookEventId, payment.Value!, ct);
    }
    public async Task<Result> UpdateSubscriptionFromPaymentAsync(Guid webhookEventId, ProviderPayment payment, CancellationToken ct = default)
    {
        var subId = ExtractSubscriptionId(payment.ExternalReference); var sub = subId.HasValue ? await subscriptions.GetByIdAsync(subId.Value, ct) : await subscriptions.GetByProviderPaymentIdAsync(payment.ProviderPaymentId, ct); if (sub is null) { await webhooks.MarkProcessedAsync(webhookEventId, null, null, null, "assinatura não localizada", ct); return Result.Failure("webhook.subscription_not_found", "Assinatura não localizada."); }
        var tx = new PaymentTransaction(Guid.NewGuid(), sub.UserId, sub.Id, PaymentProvider.MercadoPago, payment.ProviderPaymentId, payment.PreferenceId, "payment", payment.Status, payment.Amount, payment.Currency, payment.RawStatus, "{}", DateTime.UtcNow, DateTime.UtcNow); await transactions.CreateAsync(tx, ct);
        if (payment.Status == PaymentStatus.Approved) await subscriptionService.ActivateSubscriptionAsync(sub.Id, payment.ProviderPaymentId, ct); else if (payment.Status is PaymentStatus.Rejected or PaymentStatus.Failed) await subscriptions.UpdateAsync(sub with { Status = SubscriptionStatus.Failed, ProviderPaymentId = payment.ProviderPaymentId, UpdatedAt = DateTime.UtcNow }, ct); else if (payment.Status is PaymentStatus.Canceled or PaymentStatus.Refunded) await subscriptions.UpdateAsync(sub with { Status = SubscriptionStatus.Canceled, ProviderPaymentId = payment.ProviderPaymentId, CanceledAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }, ct);
        await webhooks.MarkProcessedAsync(webhookEventId, sub.UserId, sub.Id, tx.Id, null, ct); return Result.Success();
    }
    private static string? TryGet(JsonElement e, params string[] path) { foreach (var p in path) { if (e.ValueKind != JsonValueKind.Object || !e.TryGetProperty(p, out e)) return null; } return e.ValueKind == JsonValueKind.String ? e.GetString() : e.GetRawText().Trim('"'); }
    private static Guid? ExtractSubscriptionId(string? external) => Guid.TryParse(external?.Split(':').LastOrDefault(), out var id) ? id : null;
}


public sealed class FinancialDashboardService(IFinancialDashboardRepository repo, ILogger<FinancialDashboardService> logger)
{ public async Task<Result<FinancialDashboard>> GetAsync(CancellationToken ct = default) { try { return Result<FinancialDashboard>.Success(await repo.GetDashboardAsync(ct)); } catch (Exception ex) { logger.LogError(ex, "Erro ao carregar financeiro"); return Result<FinancialDashboard>.Failure("finance.error", "Não foi possível carregar o painel financeiro."); } } }
