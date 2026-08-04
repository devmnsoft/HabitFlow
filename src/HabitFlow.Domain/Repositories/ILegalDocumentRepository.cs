namespace HabitFlow.Domain;

public interface ILegalDocumentRepository
{
    Task<IReadOnlyList<LegalDocumentVersion>> ListLatestAsync(CancellationToken ct = default);
    Task<LegalDocument?> FindDocumentAsync(Guid documentId, CancellationToken ct = default);
    Task<LegalDocumentVersion?> FindVersionAsync(Guid documentId, Guid versionId, CancellationToken ct = default);
    Task<LegalDocumentVersion?> FindPublishedAsync(LegalDocumentType type, string locale, CancellationToken ct = default);
    Task<IReadOnlyList<LegalDocumentVersion>> ListVersionsAsync(Guid documentId, CancellationToken ct = default);
    Task CreateDocumentAsync(LegalDocument document, LegalDocumentVersion version, CancellationToken ct = default);
    Task UpdateDraftAsync(LegalDocumentVersion version, CancellationToken ct = default);
    Task PublishAsync(Guid documentId, Guid versionId, DateTime publishedAt, CancellationToken ct = default);
    Task ArchiveAsync(Guid documentId, Guid versionId, CancellationToken ct = default);
}
