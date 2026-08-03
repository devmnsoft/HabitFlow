namespace HabitFlow.Domain;

public enum LegalDocumentType { TermsOfUse, PrivacyNotice, CookieNotice, DataProcessingNotice, HealthDisclaimer }
public enum LegalDocumentStatus { Draft, Published, Superseded, Archived }
public enum ConsentPurpose { EmailMarketing, ProductResearch, NonNecessaryAnalytics, PromotionalCommunications }
public enum LegalAcceptanceSource { Registration, Reacceptance, PrivacyCenter, Administrative }
public enum CookieCategory { Necessary, Functional, Analytics, Marketing }
public sealed record LegalDocument(Guid Id, LegalDocumentType DocumentType, DateTime CreatedAt);
public sealed record LegalDocumentVersion(Guid Id, Guid DocumentId, string Version, string Locale, string Title, string Summary,
    string SanitizedContent, string ContentHash, DateTime EffectiveAt, DateTime? PublishedAt, bool RequiresReacceptance,
    LegalDocumentStatus Status, Guid? CreatedByUserId, DateTime CreatedAt, DateTime UpdatedAt);
public sealed record UserLegalAcceptance(Guid Id, Guid? ClientId, Guid UserId, LegalDocumentType DocumentType, string Version,
    string ContentHash, DateTime AcceptedAt, LegalAcceptanceSource Source, string CorrelationId, string? IpHmac,
    string? UserAgentHmac, DateTime? RevokedAt);
public sealed record UserConsent(Guid Id, Guid? ClientId, Guid UserId, ConsentPurpose Purpose, bool Granted,
    DateTime RecordedAt, DateTime? RevokedAt, string CorrelationId);
public sealed record CookieDescriptor(string Name, CookieCategory Category, string Purpose, TimeSpan? Duration, bool HttpOnly);
