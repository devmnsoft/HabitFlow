namespace HabitFlow.Web.Models;

public sealed record PlanLandingPageViewModel(
    IReadOnlyList<CommercialPlanCardViewModel> Plans,
    IReadOnlyList<CommercialPlanBenefitViewModel> Benefits,
    IReadOnlyList<PlanComparisonRowViewModel> Comparison,
    IReadOnlyList<PlanFaqItemViewModel> Faq,
    IReadOnlyList<PlanTrustSignalViewModel> TrustSignals,
    PlanConversionSectionViewModel Conversion)
{
    public bool HasMonthlyBilling => Plans.Any(x => x.Code != HabitFlow.Domain.PlanCodes.Free && x.MonthlyPrice is not null);
    public bool HasYearlyBilling => Plans.Any(x => x.Code != HabitFlow.Domain.PlanCodes.Free && x.YearlyPrice is not null);
}
public sealed record CommercialPlanCardViewModel(string Code, string Name, string Audience, string Description,
    string? Badge, bool Featured, string? MonthlyPrice, string? YearlyPrice, string? YearlySaving,
    IReadOnlyList<string> Benefits, string CtaLabel, string CtaUrl, bool CheckoutEligible);
public sealed record CommercialPlanBenefitViewModel(string Title, string Description, string Icon);
public sealed record PlanComparisonRowViewModel(string Benefit, string Free, string Ritmo, string? HelpText = null);
public sealed record PlanFaqItemViewModel(string Question, string Answer);
public sealed record PlanTrustSignalViewModel(string Title, string Description, string Icon);
public sealed record PlanConversionSectionViewModel(string Title, string Description, string PrimaryCta, string PrimaryUrl);
