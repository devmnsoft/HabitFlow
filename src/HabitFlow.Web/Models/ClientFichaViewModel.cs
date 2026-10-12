using HabitFlow.Application;
using HabitFlow.Domain;

namespace HabitFlow.Web.Models;

/// <summary>
/// Ficha consolidada do cliente (§4): página única com cadastro, situação, responsáveis,
/// usuários/perfis, onboarding, plano e limites, assinatura, pagamentos, suporte e
/// eventos operacionais. Nenhuma ação destrutiva é oferecida aqui — apenas ações
/// auditadas com motivo obrigatório e confirmação explícita.
/// </summary>
public sealed class ClientFichaViewModel
{
    public required Client Client { get; init; }
    public required IReadOnlyList<ClientUserSummary> Users { get; init; }
    public required ClientMetrics Metrics { get; init; }
    public required ClientEntitlementsDto? Entitlements { get; init; }
    public required IReadOnlyList<SuperAdminSubscriptionRow> Subscriptions { get; init; }
    public required IReadOnlyList<SuperAdminPaymentRow> Payments { get; init; }
    public required IReadOnlyList<SupportTicketDetail> Tickets { get; init; }
    public required IReadOnlyList<OperationalAuditEvent> Events { get; init; }
    public required IReadOnlyList<ClientOnboardingSummary> Onboarding { get; init; }

    public int Admins => Users.Count(u => string.Equals(u.Role, "Admin", StringComparison.OrdinalIgnoreCase));
    public bool HasSingleAdmin => Admins <= 1;
}

public sealed record ClientOnboardingSummary(
    Guid UserId,
    string UserName,
    OnboardingStatus Status,
    OnboardingStep CurrentStep,
    DateTime? CompletedAt,
    DateTime LastActivityAt);
