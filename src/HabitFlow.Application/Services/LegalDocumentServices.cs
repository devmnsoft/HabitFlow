using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using HabitFlow.Domain;

namespace HabitFlow.Application;

public sealed partial class LegalContentSanitizer
{
    private static readonly HashSet<string> AllowedTags = new(StringComparer.OrdinalIgnoreCase)
        { "p", "br", "strong", "em", "ul", "ol", "li", "h2", "h3", "h4", "blockquote", "a" };

    public string Sanitize(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return string.Empty;
        var value = DangerousBlock().Replace(content, string.Empty);
        value = HtmlTag().Replace(value, match => SanitizeTag(match.Value));
        return value.Trim();
    }

    private static string SanitizeTag(string tag)
    {
        var name = TagName().Match(tag).Groups[1].Value;
        if (!AllowedTags.Contains(name)) return string.Empty;
        if (tag.StartsWith("</", StringComparison.Ordinal)) return $"</{name.ToLowerInvariant()}>";
        if (name.Equals("br", StringComparison.OrdinalIgnoreCase)) return "<br>";
        if (!name.Equals("a", StringComparison.OrdinalIgnoreCase)) return $"<{name.ToLowerInvariant()}>";

        var href = Href().Match(tag).Groups[2].Value.Trim();
        if (!Uri.TryCreate(href, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeMailto)) return "<a>";
        return $"<a href=\"{System.Net.WebUtility.HtmlEncode(href)}\" rel=\"noopener noreferrer\">";
    }

    [GeneratedRegex(@"<(script|iframe|form|object|embed|style)\b[^>]*>[\s\S]*?</\1\s*>", RegexOptions.IgnoreCase)] private static partial Regex DangerousBlock();
    [GeneratedRegex(@"<[^>]*>")] private static partial Regex HtmlTag();
    [GeneratedRegex(@"^</?\s*([a-z0-9]+)", RegexOptions.IgnoreCase)] private static partial Regex TagName();
    [GeneratedRegex("href\\s*=\\s*([\\\"'])(.*?)\\1", RegexOptions.IgnoreCase)] private static partial Regex Href();
}

public sealed class LegalContentHashService
{
    public string Compute(string sanitizedContent) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(sanitizedContent)));
}

public sealed record LegalVersionDraft(string Version, string Locale, string Title, string Summary, string Content,
    DateTime EffectiveAt, bool RequiresReacceptance);

public sealed class LegalDocumentService(ILegalDocumentRepository repository, LegalContentSanitizer sanitizer, LegalContentHashService hashes, TimeProvider clock)
{
    public async Task<LegalDocumentVersion> CreateAsync(LegalDocumentType type, LegalVersionDraft draft, Guid actorId, CancellationToken ct = default)
    {
        Validate(draft);
        var document = new LegalDocument(Guid.NewGuid(), type, clock.GetUtcNow().UtcDateTime);
        var version = Build(document.Id, draft, actorId);
        await repository.CreateDocumentAsync(document, version, ct);
        return version;
    }

    public async Task<LegalDocumentVersion> UpdateDraftAsync(Guid documentId, Guid versionId, LegalVersionDraft draft, Guid actorId, CancellationToken ct = default)
    {
        Validate(draft);
        var existing = await repository.FindVersionAsync(documentId, versionId, ct) ?? throw new KeyNotFoundException("Versão jurídica não encontrada.");
        if (existing.Status != LegalDocumentStatus.Draft) throw new InvalidOperationException("Uma versão publicada é imutável; crie uma nova versão.");
        var updated = Build(documentId, draft, actorId) with { Id = versionId, CreatedAt = existing.CreatedAt };
        await repository.UpdateDraftAsync(updated, ct);
        return updated;
    }

    private LegalDocumentVersion Build(Guid documentId, LegalVersionDraft draft, Guid actorId)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var content = sanitizer.Sanitize(draft.Content);
        return new LegalDocumentVersion(Guid.NewGuid(), documentId, draft.Version.Trim(), draft.Locale.Trim(), draft.Title.Trim(),
            draft.Summary.Trim(), content, hashes.Compute(content), draft.EffectiveAt, null, draft.RequiresReacceptance,
            LegalDocumentStatus.Draft, actorId, now, now);
    }

    private static void Validate(LegalVersionDraft draft)
    {
        if (string.IsNullOrWhiteSpace(draft.Version) || string.IsNullOrWhiteSpace(draft.Title) ||
            string.IsNullOrWhiteSpace(draft.Summary) || string.IsNullOrWhiteSpace(draft.Content))
            throw new ArgumentException("Versão, título, resumo e conteúdo são obrigatórios.");
    }
}

public sealed class LegalPublicationService(ILegalDocumentRepository repository, TimeProvider clock)
{
    public async Task PublishAsync(Guid documentId, Guid versionId, CancellationToken ct = default)
    {
        var version = await repository.FindVersionAsync(documentId, versionId, ct) ?? throw new KeyNotFoundException("Versão jurídica não encontrada.");
        if (version.Status != LegalDocumentStatus.Draft) throw new InvalidOperationException("Somente rascunhos podem ser publicados.");
        await repository.PublishAsync(documentId, versionId, clock.GetUtcNow().UtcDateTime, ct);
    }

    public Task ArchiveAsync(Guid documentId, Guid versionId, CancellationToken ct = default) => repository.ArchiveAsync(documentId, versionId, ct);
}

public sealed class LegalDocumentQueryService(ILegalDocumentRepository repository)
{
    public Task<IReadOnlyList<LegalDocumentVersion>> ListAsync(CancellationToken ct = default) => repository.ListLatestAsync(ct);
    public Task<IReadOnlyList<LegalDocumentVersion>> VersionsAsync(Guid id, CancellationToken ct = default) => repository.ListVersionsAsync(id, ct);
    public Task<LegalDocumentVersion?> VersionAsync(Guid id, Guid versionId, CancellationToken ct = default) => repository.FindVersionAsync(id, versionId, ct);
    public Task<LegalDocumentVersion?> PublishedAsync(LegalDocumentType type, CancellationToken ct = default) => repository.FindPublishedAsync(type, "pt-BR", ct);
}

public sealed record LegalVersionDifference(bool TitleChanged, bool SummaryChanged, bool ContentChanged, bool ReacceptanceChanged);
public sealed class LegalDocumentVersionComparer
{
    public LegalVersionDifference Compare(LegalDocumentVersion previous, LegalDocumentVersion current) => new(
        previous.Title != current.Title, previous.Summary != current.Summary,
        previous.ContentHash != current.ContentHash, previous.RequiresReacceptance != current.RequiresReacceptance);
}
