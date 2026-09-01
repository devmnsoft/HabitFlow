namespace HabitFlow.Domain;

public sealed record AssistantConversation(Guid Id, Guid ClientId, Guid UserId, DateTime CreatedAt, DateTime UpdatedAt);
public sealed record AssistantMessage(Guid Id, Guid ClientId, Guid UserId, Guid ConversationId, string Role, string Message, string SanitizedMessage, string SafetyStatus, string Provider, DateTime CreatedAt, string CorrelationId);
public sealed record SupportSettings(Guid Id, string CompanyName, string CompanyDocument, string SupportEmail, string? WhatsAppPhone, string DefaultMessage, string BusinessHours, bool IsActive, string ButtonText, DateTime UpdatedAt);
public sealed record SupportTicketDetail(Guid Id, Guid ClientId, Guid UserId, string Protocol, string Category, string Priority, string Status, string Subject, string Description, string SafeContext, Guid? AssignedUserId, DateTime SlaDueAt, DateTime CreatedAt, DateTime UpdatedAt, DateTime? ClosedAt);
public sealed record SupportTicketMessage(Guid Id, Guid ClientId, Guid TicketId, Guid UserId, bool IsStaff, bool IsInternal, string Message, DateTime CreatedAt);
public sealed record SupportTicketHistory(Guid Id, Guid ClientId, Guid TicketId, Guid? ActorUserId, string FromStatus, string ToStatus, string? Reason, DateTime CreatedAt);

public interface IAssistanceRepository
{
    Task<Guid> GetOrCreateConversationAsync(Guid clientId, Guid userId, CancellationToken ct = default);
    Task AddMessageAsync(AssistantMessage message, CancellationToken ct = default);
    Task<IReadOnlyList<AssistantMessage>> ListMessagesAsync(Guid clientId, Guid userId, Guid conversationId, CancellationToken ct = default);
    Task DeleteHistoryAsync(Guid clientId, Guid userId, CancellationToken ct = default);
    Task<SupportSettings> GetSupportSettingsAsync(CancellationToken ct = default);
    Task UpdateSupportSettingsAsync(SupportSettings settings, CancellationToken ct = default);
    Task CreateTicketAsync(SupportTicketDetail ticket, CancellationToken ct = default);
    Task<IReadOnlyList<SupportTicketDetail>> ListTicketsAsync(Guid clientId, Guid userId, bool admin, CancellationToken ct = default);
    Task<SupportTicketDetail?> GetTicketAsync(Guid clientId, Guid userId, Guid ticketId, bool admin, CancellationToken ct = default);
    Task AddTicketMessageAsync(SupportTicketMessage message, CancellationToken ct = default);
    Task<IReadOnlyList<SupportTicketMessage>> ListTicketMessagesAsync(Guid clientId, Guid ticketId, CancellationToken ct = default);
    Task UpdateTicketStatusAsync(Guid clientId, Guid ticketId, string status, DateTime? closedAt, CancellationToken ct = default);
    Task ReopenTicketAsync(Guid clientId, Guid ticketId, Guid actorUserId, string reason, DateTime slaDueAt, CancellationToken ct = default);
    Task<IReadOnlyList<SupportTicketHistory>> ListTicketHistoryAsync(Guid clientId, Guid ticketId, CancellationToken ct = default);
}
