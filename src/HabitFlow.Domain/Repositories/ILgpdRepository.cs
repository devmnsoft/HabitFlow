namespace HabitFlow.Domain;

public interface ILgpdRepository
{
    Task CreateAsync(LgpdRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<LgpdRequest>> ListByUserAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<UserPrivacyConsent>> ListConsentsAsync(Guid userId, CancellationToken ct = default);
    Task UpsertConsentAsync(UserPrivacyConsent consent, CancellationToken ct = default);
}
