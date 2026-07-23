namespace HabitFlow.Domain;

public interface IBillingStatusRepository
{
    Task<int> MarkOverdueInvoicesAsync(DateOnly today, CancellationToken ct = default);
    Task<int> BlockBenefitsAfterGracePeriodAsync(DateOnly today, int gracePeriodDays, CancellationToken ct = default);
    Task ReactivateClientAfterApprovedPaymentAsync(Guid clientId, string benefitsStatus, CancellationToken ct = default);
}
