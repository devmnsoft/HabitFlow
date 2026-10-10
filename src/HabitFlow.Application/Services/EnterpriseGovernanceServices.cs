using System.Net;
using System.Text.RegularExpressions;
using HabitFlow.Domain;

namespace HabitFlow.Application;

public sealed class EnterpriseSsoPolicyService
{
    public IReadOnlyList<EnterprisePolicyIssue> Validate(TenantSsoConfiguration config, bool hasEntitlement)
    {
        var issues = new List<EnterprisePolicyIssue>();
        if (!config.Enabled) return issues;

        if (!hasEntitlement)
            issues.Add(Issue("sso.entitlement", "Critical", "SSO so pode ser ativado para tenants com entitlement Enterprise."));
        if (config.Provider is EnterpriseSsoProvider.None)
            issues.Add(Issue("sso.provider", "High", "Selecione um provedor SSO suportado."));
        if (!Uri.TryCreate(config.Authority, UriKind.Absolute, out var authority) || authority.Scheme != Uri.UriSchemeHttps)
            issues.Add(Issue("sso.authority", "High", "Authority deve ser uma URL HTTPS valida."));
        if (string.IsNullOrWhiteSpace(config.ClientId))
            issues.Add(Issue("sso.client_id", "High", "ClientId e obrigatorio; client secret deve vir somente de variavel de ambiente."));
        if (string.IsNullOrWhiteSpace(config.CallbackPath) || !config.CallbackPath.StartsWith('/') || config.CallbackPath.Contains("//", StringComparison.Ordinal))
            issues.Add(Issue("sso.callback", "High", "CallbackPath deve ser um caminho local seguro."));
        if (!config.RequireSignedTokens)
            issues.Add(Issue("sso.signed_tokens", "Critical", "Tokens assinados sao obrigatorios."));

        return issues;
    }

    public bool CanUseLocalFallback(TenantSsoConfiguration config, bool tenantAllowsFallback) =>
        !config.Enabled || config.AllowLocalLoginFallback && tenantAllowsFallback;

    private static EnterprisePolicyIssue Issue(string code, string severity, string message) => new(code, severity, message);
}

public sealed class TenantBrandingPolicyService
{
    public IReadOnlyList<EnterprisePolicyIssue> Validate(TenantBranding branding, bool hasEntitlement)
    {
        var issues = new List<EnterprisePolicyIssue>();
        if (!hasEntitlement)
            issues.Add(new("branding.entitlement", "High", "White label exige entitlement do plano."));
        if (string.IsNullOrWhiteSpace(branding.CommercialName))
            issues.Add(new("branding.name", "Medium", "Nome comercial e obrigatorio."));
        if (!IsHexColor(branding.PrimaryColor) || !IsHexColor(branding.SecondaryColor))
            issues.Add(new("branding.color", "High", "Cores devem usar formato hexadecimal."));
        else if (ContrastRatio(branding.PrimaryColor, "#ffffff") < 4.5m)
            issues.Add(new("branding.contrast", "High", "Cor primaria nao possui contraste minimo com texto claro para botoes e links principais."));
        if (branding.SupportUrl is { Length: > 0 } && !IsHttpsUrl(branding.SupportUrl))
            issues.Add(new("branding.support_url", "Medium", "Link de suporte deve usar HTTPS."));
        if (branding.LogoPath is { Length: > 0 } && !IsSafeAssetPath(branding.LogoPath))
            issues.Add(new("branding.logo", "High", "Logo deve apontar para asset validado do tenant."));
        return issues;
    }

    private static bool IsHexColor(string value) => Regex.IsMatch(value, "^#[0-9a-fA-F]{6}$", RegexOptions.CultureInvariant);
    private static bool IsHttpsUrl(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
    private static bool IsSafeAssetPath(string value) => value.StartsWith("/uploads/tenant-branding/", StringComparison.Ordinal) && !value.Contains("..", StringComparison.Ordinal);

    private static decimal ContrastRatio(string left, string right)
    {
        var a = RelativeLuminance(left);
        var b = RelativeLuminance(right);
        var lighter = Math.Max(a, b);
        var darker = Math.Min(a, b);
        return (lighter + 0.05m) / (darker + 0.05m);
    }

    private static decimal RelativeLuminance(string color)
    {
        static decimal Channel(int value)
        {
            var normalized = value / 255m;
            return normalized <= 0.03928m ? normalized / 12.92m : (decimal)Math.Pow((double)((normalized + 0.055m) / 1.055m), 2.4);
        }

        var r = Convert.ToInt32(color.Substring(1, 2), 16);
        var g = Convert.ToInt32(color.Substring(3, 2), 16);
        var b = Convert.ToInt32(color.Substring(5, 2), 16);
        return 0.2126m * Channel(r) + 0.7152m * Channel(g) + 0.0722m * Channel(b);
    }
}

public sealed class TenantDomainPolicyService
{
    public IReadOnlyList<EnterprisePolicyIssue> Validate(TenantDomain domain, IEnumerable<TenantDomain> existingDomains, bool hasEntitlement)
    {
        var issues = new List<EnterprisePolicyIssue>();
        var normalized = Normalize(domain.HostName);
        if (!hasEntitlement)
            issues.Add(new("domain.entitlement", "High", "Dominio personalizado exige entitlement Enterprise."));
        if (!IsValidHost(normalized))
            issues.Add(new("domain.invalid", "High", "Dominio invalido ou reservado."));
        if (existingDomains.Any(x => x.TenantId != domain.TenantId && Normalize(x.HostName).Equals(normalized, StringComparison.OrdinalIgnoreCase)))
            issues.Add(new("domain.duplicate", "Critical", "Dominio ja esta vinculado a outro tenant."));
        if (domain.Status == TenantDomainStatus.Active && !CanActivate(domain))
            issues.Add(new("domain.not_verified", "Critical", "Dominio nao pode ser ativado sem verificacao DNS."));
        return issues;
    }

    public static bool CanActivate(TenantDomain domain) => domain.Status is TenantDomainStatus.Verified or TenantDomainStatus.Active;

    public static string Normalize(string host) => host.Trim().TrimEnd('.').ToLowerInvariant();

    private static bool IsValidHost(string host)
    {
        if (host.Length is < 4 or > 253 || host.Contains('/') || host.Contains(':') || host.StartsWith("*.") || host.Contains("..")) return false;
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || IPAddress.TryParse(host, out _)) return false;
        return Regex.IsMatch(host, "^(?!-)([a-z0-9-]{1,63}\\.)+[a-z]{2,63}$", RegexOptions.CultureInvariant);
    }
}

public sealed class EnterpriseAiPolicyService
{
    public IReadOnlyList<EnterprisePolicyIssue> Validate(EnterpriseAiPolicy policy, IReadOnlySet<string> allowedProviders, IReadOnlySet<string> allowedModels, bool hasEntitlement)
    {
        var issues = new List<EnterprisePolicyIssue>();
        if (!policy.Enabled) return issues;
        if (!hasEntitlement)
            issues.Add(new("ai.entitlement", "High", "IA corporativa exige entitlement do plano."));
        if (!allowedProviders.Contains(policy.Provider))
            issues.Add(new("ai.provider", "High", "Provider de IA nao esta habilitado."));
        if (!allowedModels.Contains(policy.Model))
            issues.Add(new("ai.model", "Critical", "Modelo de IA fora da lista permitida."));
        if (policy.AllowSensitiveData)
            issues.Add(new("ai.sensitive_data", "High", "Envio de dados sensiveis exige consentimento e configuracao explicita."));
        if (policy.DailyUserLimit <= 0)
            issues.Add(new("ai.limit", "Medium", "Limite diario por usuario deve ser positivo."));
        return issues;
    }
}

