namespace HabitFlow.Application;

public sealed record RegisterClientAccountDto(
    string ClientPersonType,
    string DocumentType,
    string DocumentRaw,
    string DocumentNormalized,
    string ClientName,
    string? LegalName,
    string? TradeName,
    string? ResponsibleName,
    string Email,
    string? Phone,
    string Password,
    bool AcceptedTerms,
    bool AcceptedPrivacy);
