namespace HabitFlow.Web.Models;

public sealed record PlanLandingPageViewModel(
    IReadOnlyList<CommercialPlanCardViewModel> Plans,
    IReadOnlyList<CommercialPlanBenefitViewModel> Benefits,
    IReadOnlyList<PlanComparisonRowViewModel> Comparison,
    IReadOnlyList<PlanFaqItemViewModel> Faq,
    IReadOnlyList<PlanTrustSignalViewModel> TrustSignals,
    PlanConversionSectionViewModel Conversion)
{
    public PlanViewerViewModel Viewer { get; init; } = PlanViewerViewModel.Visitor;
    public bool HasMonthlyBilling => Plans.Any(x => x.Code != HabitFlow.Domain.PlanCodes.Free && x.MonthlyPrice is not null);
    public bool HasYearlyBilling => Plans.Any(x => x.Code != HabitFlow.Domain.PlanCodes.Free && x.YearlyPrice is not null);
}
public sealed record PlanViewerViewModel(bool IsAuthenticated, string PlanCode, string? BillingCycle, string Status, string StatusMessage,
    DateTime? AccessUntil, DateTime? NextBillingAt, bool CanManage)
{
    public static readonly PlanViewerViewModel Visitor = new(false, "visitor", null, "Visitante", "Crie sua conta grátis ou entre para assinar.", null, null, false);
    public bool IsCurrent(string code, string? cycle = null) => IsAuthenticated && PlanCode.Equals(code, StringComparison.OrdinalIgnoreCase)
        && (cycle is null || BillingCycle is null || BillingCycle.Equals(cycle, StringComparison.OrdinalIgnoreCase));
}
public sealed record CommercialPlanCardViewModel(string Code, string Name, string Audience, string Description,
    string? Badge, bool Featured, string? MonthlyPrice, string? YearlyPrice, string? YearlySaving,
    IReadOnlyList<string> Benefits, string CtaLabel, string CtaUrl, bool CheckoutEligible);
public sealed record CommercialPlanBenefitViewModel(string Title, string Description, string Icon);
public sealed record PlanComparisonRowViewModel(string Benefit, string Free, string Ritmo, string? HelpText = null);
public sealed record PlanFaqItemViewModel(string Question, string Answer);
public sealed record PlanTrustSignalViewModel(string Title, string Description, string Icon);
public sealed record PlanConversionSectionViewModel(string Title, string Description, string PrimaryCta, string PrimaryUrl);
