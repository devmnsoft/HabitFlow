namespace HabitFlow.Domain;

public interface ISupportRepository
{
    Task CreateTicketAsync(SupportTicket ticket, CancellationToken ct = default);
    Task AddMessageAsync(SupportMessage message, CancellationToken ct = default);
    Task<IReadOnlyList<SupportTicket>> ListByUserAsync(Guid userId, CancellationToken ct = default);
}
