namespace HabitFlow.Domain;

public interface ILgpdRepository
{
    Task CreateAsync(LgpdRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<LgpdRequest>> ListByUserAsync(Guid clientId, Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<UserPrivacyConsent>> ListConsentsAsync(Guid clientId, Guid userId, CancellationToken ct = default);
    Task UpsertConsentAsync(UserPrivacyConsent consent, CancellationToken ct = default);
    Task<string> ExportOwnedDataJsonAsync(Guid clientId, Guid userId, CancellationToken ct = default);
    Task RecordSecurityEventAsync(Guid clientId, Guid userId, string eventType, string severity, CancellationToken ct = default);
}
