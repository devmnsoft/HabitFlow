using HabitFlow.Domain;

namespace HabitFlow.Application;

public sealed class PlanFeatureImplementationRegistry
{
    private static readonly IReadOnlyDictionary<string, PlanFeatureImplementation> Items = Build()
        .ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<PlanFeatureImplementation> All => Items.Values;
    public PlanFeatureImplementation? Find(string code) => Items.GetValueOrDefault(code);

    private static IEnumerable<PlanFeatureImplementation> Build()
    {
        static PlanFeatureImplementation I(string code, string name, string description, string evidence, string[] routes, string[] services) =>
            new(code, PlanFeatureImplementationStatus.Implemented, name, description, evidence, true, "6.9", routes, services);
        static PlanFeatureImplementation N(string code, PlanFeatureImplementationStatus status, string evidence) =>
            new(code, status, string.Empty, string.Empty, evidence, false, null, [], []);
        yield return I(PlanFeatureCodes.ActiveHabitsLimit, "Hábitos ativos", "Limite de hábitos ativos do plano.", "HabitPolicy e CreateHabitFromTemplateUseCase", ["/habits"], ["HabitPolicy"]);
        yield return I(PlanFeatureCodes.ActiveGoalsLimit, "Objetivos ativos", "Limite de objetivos ativos do plano.", "GoalService", ["/goals"], ["GoalService"]);
        yield return I(PlanFeatureCodes.FullHabitLibrary, "Biblioteca completa", "Acesso ao catálogo completo de hábitos.", "HabitLibraryService", ["/habit-library"], ["HabitLibraryService"]);
        yield return I(PlanFeatureCodes.BasicReports, "Relatório básico", "Resumo semanal e consistência básica.", "ReportService", ["/reports"], ["ReportService"]);
        yield return I(PlanFeatureCodes.ReportExportCsv, "Exportação CSV", "Exporte relatórios em formato CSV.", "ReportsController.Export", ["/reports/export"], ["ReportService"]);
        yield return I(PlanFeatureCodes.ReportPrint, "Versão para impressão", "Relatórios preparados para impressão.", "ReportsController.Print", ["/reports/print"], ["ReportService"]);
        yield return I(PlanFeatureCodes.FullHistory, "Histórico completo", "Consulte todo o histórico disponível.", "ProgressPeriodAccess", ["/progress"], ["PlanEntitlementService"]);
        yield return I(PlanFeatureCodes.HistoryDaysLimit, "Histórico", "Período de histórico conforme o plano.", "ProgressCalendarService", ["/progress"], ["ProgressCalendarService"]);
        yield return I(PlanFeatureCodes.CustomCategories, "Categorias personalizadas", "Organize hábitos com categorias próprias.", "HabitsController", ["/habits/create"], ["HabitService"]);
        yield return N(PlanFeatureCodes.RemindersPerHabit, PlanFeatureImplementationStatus.Partial, "CRUD disponível; dispatch completo e operação IIS ainda não comprovados.");
        yield return N(PlanFeatureCodes.AdvancedReports, PlanFeatureImplementationStatus.Partial, "Não reúne ainda toda a definição comercial de análise avançada.");
        yield return N(PlanFeatureCodes.SharedRoutines, PlanFeatureImplementationStatus.Partial, "Compartilhamento não possui jornada comercial completa.");
        yield return N(PlanFeatureCodes.SharedGoals, PlanFeatureImplementationStatus.Planned, "Sem colaboração completa.");
        yield return N(PlanFeatureCodes.ConsolidatedReports, PlanFeatureImplementationStatus.Planned, "Sem consolidação real entre membros.");
        yield return N(PlanFeatureCodes.UserInvitations, PlanFeatureImplementationStatus.Internal, "Capacidade administrativa, não benefício público.");
        yield return N(PlanFeatureCodes.ClientAdminDashboard, PlanFeatureImplementationStatus.Internal, "Painel interno.");
        yield return N(PlanFeatureCodes.PrioritySupport, PlanFeatureImplementationStatus.Planned, "Sem SLA próprio.");
        yield return N(PlanFeatureCodes.InternalCommunications, PlanFeatureImplementationStatus.Internal, "Operação interna.");
        yield return N(PlanFeatureCodes.UsersLimit, PlanFeatureImplementationStatus.Internal, "Entitlement de conta.");
    }
}

public sealed class PlanPublicBenefitService(PlanFeatureImplementationRegistry registry)
{
    public IReadOnlyList<PlanPublicBenefit> Validate(string planCode, IEnumerable<PlanPublicBenefit> candidates) => candidates
        .Where(x => string.Equals(x.PlanCode, planCode, StringComparison.OrdinalIgnoreCase))
        .Where(x => registry.Find(x.FeatureCode) is { Status: PlanFeatureImplementationStatus.Implemented, IsMarketable: true })
        .OrderBy(x => x.SortOrder).ToList();
}

public sealed class PlanCatalogValidationService(PlanFeatureImplementationRegistry registry)
{
    public IReadOnlyList<string> Validate(IEnumerable<string> publicFeatureCodes) => publicFeatureCodes
        .Where(code => registry.Find(code) is not { Status: PlanFeatureImplementationStatus.Implemented, IsMarketable: true })
        .Select(code => $"Feature pública inválida: {code}").ToList();
}
