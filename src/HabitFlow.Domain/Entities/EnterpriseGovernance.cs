namespace HabitFlow.Domain;

public enum EnterpriseSsoProvider { None, OpenIdConnect, OAuth2, Saml }
public enum TenantDomainStatus { NotConfigured, WaitingDns, Verified, Active, Error, Suspended }
public enum LegalDocumentAudience { Global, Tenant, User }

public sealed record TenantSsoConfiguration(
    Guid TenantId,
    bool Enabled,
    EnterpriseSsoProvider Provider,
    string Authority,
    string ClientId,
    string CallbackPath,
    bool RequireSignedTokens,
    bool AllowLocalLoginFallback,
    string? TenantClaim,
    string? LastSanitizedError);

public sealed record TenantMfaPolicy(Guid TenantId, bool RequireForOwners, bool RequireForAdmins, bool RequireForSensitiveRoles);

public sealed record TenantBranding(
    Guid TenantId,
    string CommercialName,
    string? LogoPath,
    string? FaviconPath,
    string PrimaryColor,
    string SecondaryColor,
    string? SupportUrl,
    string? WelcomeText,
    DateTime UpdatedAt);

public sealed record TenantDomain(Guid TenantId, string HostName, TenantDomainStatus Status, bool IsPrimary, DateTime UpdatedAt);

public sealed record EnterpriseAiPolicy(
    Guid TenantId,
    bool Enabled,
    string Provider,
    string Model,
    bool AllowSensitiveData,
    int DailyUserLimit,
    DateTime UpdatedAt);

public sealed record EnterprisePolicyIssue(string Code, string Severity, string Message);

