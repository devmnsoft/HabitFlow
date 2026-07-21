namespace HabitFlow.Domain;

public interface IBillingRepository
{
    Task AddAsync(BillingEvent billingEvent, CancellationToken ct = default);
}
