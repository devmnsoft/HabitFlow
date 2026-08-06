using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class LegalDocumentRepository(SqlExecutor db) : ILegalDocumentRepository
{
    public async Task<IReadOnlyList<LegalDocumentVersion>> ListLatestAsync(CancellationToken ct = default) =>
        (await db.QueryAsync<LegalDocumentVersion>(VersionSelect + " order by v.updated_at desc", ct: ct)).ToList();

    public Task<LegalDocument?> FindDocumentAsync(Guid documentId, CancellationToken ct = default) =>
        db.QuerySingleOrDefaultAsync<LegalDocument>("select id, document_type, created_at from habitflow.legal_documents where id=@documentId", new { documentId }, ct);

    public Task<LegalDocumentVersion?> FindVersionAsync(Guid documentId, Guid versionId, CancellationToken ct = default) =>
        db.QuerySingleOrDefaultAsync<LegalDocumentVersion>(VersionSelect + " where v.document_id=@documentId and v.id=@versionId", new { documentId, versionId }, ct);

    public Task<LegalDocumentVersion?> FindPublishedAsync(LegalDocumentType type, string locale, CancellationToken ct = default) =>
        db.QuerySingleOrDefaultAsync<LegalDocumentVersion>(VersionSelect + " join habitflow.legal_documents d on d.id=v.document_id where d.document_type=@type and v.locale=@locale and v.status='Published' and v.effective_at<=now() order by v.effective_at desc limit 1", new { type = DbEnum.Text(type), locale }, ct);

    public async Task<IReadOnlyList<LegalDocumentVersion>> ListVersionsAsync(Guid documentId, CancellationToken ct = default) =>
        (await db.QueryAsync<LegalDocumentVersion>(VersionSelect + " where v.document_id=@documentId order by v.created_at desc", new { documentId }, ct)).ToList();

    public async Task CreateDocumentAsync(LegalDocument document, LegalDocumentVersion version, CancellationToken ct = default)
    {
        await db.ExecuteAsync("insert into habitflow.legal_documents(id,document_type,created_at) values(@Id,@DocumentType,@CreatedAt)", new { document.Id, DocumentType = DbEnum.Text(document.DocumentType), document.CreatedAt }, ct);
        await InsertVersionAsync(version, ct);
    }

    private Task InsertVersionAsync(LegalDocumentVersion version, CancellationToken ct) => db.ExecuteAsync(
        "insert into habitflow.legal_document_versions(id,document_id,version,locale,title,summary,sanitized_content,content_hash,effective_at,published_at,requires_reacceptance,status,created_by_user_id,created_at,updated_at) values(@Id,@DocumentId,@Version,@Locale,@Title,@Summary,@SanitizedContent,@ContentHash,@EffectiveAt,@PublishedAt,@RequiresReacceptance,@Status,@CreatedByUserId,@CreatedAt,@UpdatedAt)",
        new { version.Id, version.DocumentId, version.Version, version.Locale, version.Title, version.Summary, version.SanitizedContent, version.ContentHash, version.EffectiveAt, version.PublishedAt, version.RequiresReacceptance, Status = DbEnum.Text(version.Status), version.CreatedByUserId, version.CreatedAt, version.UpdatedAt }, ct);

    public Task UpdateDraftAsync(LegalDocumentVersion version, CancellationToken ct = default) => db.ExecuteAsync(
        "update habitflow.legal_document_versions set version=@Version,locale=@Locale,title=@Title,summary=@Summary,sanitized_content=@SanitizedContent,content_hash=@ContentHash,effective_at=@EffectiveAt,requires_reacceptance=@RequiresReacceptance,updated_at=@UpdatedAt where id=@Id and document_id=@DocumentId and status='Draft'",
        version, ct);

    public Task PublishAsync(Guid documentId, Guid versionId, DateTime publishedAt, CancellationToken ct = default) => db.ExecuteAsync(
        "update habitflow.legal_document_versions set status='Superseded',updated_at=@publishedAt where document_id=@documentId and status='Published'; update habitflow.legal_document_versions set status='Published',published_at=@publishedAt,updated_at=@publishedAt where id=@versionId and document_id=@documentId and status='Draft'",
        new { documentId, versionId, publishedAt }, ct);

    public Task ArchiveAsync(Guid documentId, Guid versionId, CancellationToken ct = default) => db.ExecuteAsync(
        "update habitflow.legal_document_versions set status='Archived',updated_at=now() where id=@versionId and document_id=@documentId and status in ('Draft','Superseded')", new { documentId, versionId }, ct);

    private const string VersionSelect = "select v.id,v.document_id,v.version,v.locale,v.title,v.summary,v.sanitized_content,v.content_hash,v.effective_at,v.published_at,v.requires_reacceptance,v.status,v.created_by_user_id,v.created_at,v.updated_at from habitflow.legal_document_versions v";
}
