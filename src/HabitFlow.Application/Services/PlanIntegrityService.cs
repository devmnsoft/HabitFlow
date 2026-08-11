using System.Globalization;
using System.Text;
using HabitFlow.Domain;

namespace HabitFlow.Application;

public sealed record PlanIntegrityIssue(string PlanCode, string Code, string Message, string Severity = "Error");
public sealed record PlanIntegrityPlan(string Code, string Name, decimal? Monthly, decimal? Yearly, bool IsPublic,
    bool IsSellable, string SalesStatus, IReadOnlyList<string> ImplementedFeatures, IReadOnlyList<string> BlockedFeatures);
public sealed record PlanIntegrityReport(IReadOnlyList<PlanIntegrityPlan> Plans, IReadOnlyList<PlanIntegrityIssue> Issues)
{
    public bool IsValid => Issues.All(x => !x.Severity.Equals("Error", StringComparison.OrdinalIgnoreCase));
}

public sealed class PlanFeatureImplementationVerifier
{
    public PlanIntegrityIssue? Verify(PlanIntegrityCatalogItem item)
    {
        if (string.IsNullOrWhiteSpace(item.FeatureCode) || item.BoolValue == false) return null;
        if (item.IsSellable && item.IsPublic &&
            (!string.Equals(item.ImplementationStatus, "Implemented", StringComparison.OrdinalIgnoreCase) || item.IsMarketable != true))
            return new(item.Code, "feature.not_market_ready", $"{item.FeatureCode} é oferecida por um plano vendável sem estar Implemented e marketable.");
        return null;
    }

    public IReadOnlyList<PlanIntegrityIssue> VerifyEvidence(string planCode, PlanFeatureImplementation feature,
        IReadOnlySet<string> availableRoutes, IReadOnlySet<string> availableServices)
    {
        if (feature.Status != PlanFeatureImplementationStatus.Implemented || !feature.IsMarketable) return [];
        var issues = feature.RequiredRoutes.Where(route => !availableRoutes.Contains(route))
            .Select(route => new PlanIntegrityIssue(planCode, "evidence.route_missing", $"{feature.Code} exige a rota ausente {route}."))
            .Concat(feature.RequiredServices.Where(service => !availableServices.Contains(service))
                .Select(service => new PlanIntegrityIssue(planCode, "evidence.service_missing", $"{feature.Code} exige o serviço ausente {service}.")))
            .ToArray();
        return issues;
    }
}

public sealed class PlanIntegrityService(IPlanCatalogRepository repository, PlanFeatureImplementationVerifier verifier)
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public async Task<PlanIntegrityReport> AuditAsync(CancellationToken ct = default) =>
        Audit(await repository.GetIntegrityCatalogAsync(ct));

    public PlanIntegrityReport Audit(IReadOnlyList<PlanIntegrityCatalogItem> catalog)
    {
        var issues = catalog.Select(verifier.Verify).Where(x => x is not null).Cast<PlanIntegrityIssue>().ToList();
        var plans = catalog.GroupBy(x => new { x.Code, x.PublicName, x.IsPublic, x.IsSellable, x.SalesStatus }).Select(group =>
        {
            decimal? Price(string cycle) => group.FirstOrDefault(x => string.Equals(x.BillingCycle, cycle, StringComparison.OrdinalIgnoreCase))?.Amount;
            var offered = group.Where(IsOfferedFeature).DistinctBy(x => x.FeatureCode).ToArray();
            var implemented = offered.Where(IsMarketReady).Select(x => x.FeatureName ?? x.FeatureCode!).ToArray();
            var blocked = offered.Where(x => !IsMarketReady(x)).Select(x => $"{x.FeatureName ?? x.FeatureCode} ({x.ImplementationStatus ?? "Unknown"})").ToArray();
            if (group.Key.IsSellable && !Price("Monthly").HasValue && !Price("Yearly").HasValue)
                issues.Add(new(group.Key.Code, "price.missing", "Plano vendável não possui preço mensal ou anual ativo."));
            if (group.Key.Code.Equals(PlanCodes.Free, StringComparison.OrdinalIgnoreCase) && group.Key.IsSellable)
                issues.Add(new(group.Key.Code, "free.sellable", "O plano gratuito não pode iniciar checkout pago."));
            return new PlanIntegrityPlan(group.Key.Code, group.Key.PublicName, Price("Monthly"), Price("Yearly"), group.Key.IsPublic,
                group.Key.IsSellable, group.Key.SalesStatus, implemented, blocked);
        }).ToArray();
        return new(plans, issues);
    }

    public string GenerateMarkdown(PlanIntegrityReport report)
    {
        var text = new StringBuilder("# Relatório de integridade comercial v6.12.1\n\n");
        foreach (var plan in report.Plans)
        {
            string Money(decimal? value) => value.HasValue ? value.Value.ToString("C", PtBr) : "Indisponível";
            text.AppendLine($"## {plan.Name} (`{plan.Code}`)").AppendLine()
                .AppendLine($"- **Mensal:** {Money(plan.Monthly)}").AppendLine($"- **Anual:** {Money(plan.Yearly)}")
                .AppendLine($"- **Público:** {(plan.IsPublic ? "Sim" : "Não")}").AppendLine($"- **Vendável:** {(plan.IsSellable ? "Sim" : "Não")}")
                .AppendLine($"- **Status de venda:** {plan.SalesStatus}")
                .AppendLine($"- **Features implementadas:** {(plan.ImplementedFeatures.Count > 0 ? string.Join(", ", plan.ImplementedFeatures) : "Nenhuma")}")
                .AppendLine($"- **Features bloqueadas:** {(plan.BlockedFeatures.Count > 0 ? string.Join(", ", plan.BlockedFeatures) : "Nenhuma")}")
                .AppendLine($"- **Problemas encontrados:** {IssueText(report, plan.Code)}")
                .AppendLine("- **Correções aplicadas:** oferta pública filtrada por `Implemented` e `is_marketable=true`; checkout condicionado a plano/ciclo elegível.").AppendLine();
        }
        return text.AppendLine($"## Resultado\n\n{(report.IsValid ? "Integridade aprovada." : "Integridade reprovada; consulte os problemas acima.")}").ToString();
    }

    private static string IssueText(PlanIntegrityReport report, string code) =>
        string.Join("; ", report.Issues.Where(x => x.PlanCode == code).Select(x => x.Message)) is { Length: > 0 } value ? value : "Nenhum";
    private static bool IsOfferedFeature(PlanIntegrityCatalogItem item) => item.BoolValue == true || item.IntValue is not null || !string.IsNullOrWhiteSpace(item.StringValue);
    private static bool IsMarketReady(PlanIntegrityCatalogItem item) => string.Equals(item.ImplementationStatus, "Implemented", StringComparison.OrdinalIgnoreCase) && item.IsMarketable == true;
}
