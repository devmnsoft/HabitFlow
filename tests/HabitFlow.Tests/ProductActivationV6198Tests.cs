using HabitFlow.Application;
using HabitFlow.Domain;
using Xunit;

namespace HabitFlow.Tests;

public sealed class ProductActivationV6198Tests
{
    private static readonly string Root = RepositoryRootLocator.Root;

    [Fact]
    public void Checklist_does_not_mark_support_done_without_evidence()
    {
        var steps = ImplantationChecklist.Evaluate(Facts());
        var support = Assert.Single(steps, step => step.Code == "support");
        Assert.Equal(ImplantationStepStatus.Pendente, support.Status);
        Assert.Contains("não disponível", support.Reason);
    }

    [Fact]
    public void Checklist_ignore_requires_reason_and_blocks_finish()
    {
        var invalid = ImplantationChecklist.Ignore("support", "não");
        Assert.True(invalid.IsFailure);
        var steps = ImplantationChecklist.Evaluate(Facts() with
        {
            Overrides = [new ImplantationOverride("support", ImplantationStepStatus.Ignorado, "Canal externo já combinado")]
        });
        Assert.Equal(ImplantationStepStatus.Ignorado, steps.Single(step => step.Code == "support").Status);
        Assert.False(ImplantationChecklist.CanFinish(steps));
    }

    [Fact]
    public void Checklist_ai_is_blocked_when_plan_disallows_it()
    {
        var ai = ImplantationChecklist.Evaluate(Facts() with { PlanAllowsAi = false }).Single(step => step.Code == "ai");
        Assert.Equal(ImplantationStepStatus.Bloqueado, ai.Status);
    }

    [Fact]
    public void Template_global_is_protected_and_tenant_is_isolated()
    {
        var tenant = Guid.NewGuid();
        var other = Guid.NewGuid();
        Assert.False(HabitTemplateAccess.CanEdit(false, null, tenant).Allowed);
        Assert.True(HabitTemplateAccess.CanEdit(true, null, null).Allowed);
        Assert.False(HabitTemplateAccess.CanUse(other, tenant, "team", "free", 0, 10, false, false).Allowed);
        Assert.True(HabitTemplateAccess.CanUse(tenant, tenant, "team", "free", 0, 10, false, false).Allowed);
    }

    [Fact]
    public void Template_free_limit_and_duplicate_confirmation_are_enforced()
    {
        var client = Guid.NewGuid();
        Assert.Equal("template.habit_limit", HabitTemplateAccess.CanUse(null, client, "free", "free", 5, 5, false, false).Code);
        Assert.Equal("template.duplicate_confirmation", HabitTemplateAccess.CanUse(null, client, "free", "free", 1, 5, true, false).Code);
        Assert.True(HabitTemplateAccess.CanUse(null, client, "free", "free", 1, 5, true, true).Allowed);
        Assert.False(HabitTemplateAccess.CanOwnTemplates("free"));
        Assert.True(HabitTemplateAccess.CanOwnTemplates("team"));
        Assert.False(HabitTemplateAccess.MeetsMinimumPlan("free", "team"));
    }

    [Fact]
    public void Customer_success_marks_missing_metrics_unavailable()
    {
        var page = CustomerSuccessBoard.Build([], false, false, false);
        Assert.Contains(page.Indicators, metric => metric.Label == "Uso de IA" && metric.Display == "não disponível");
        Assert.Contains(page.Indicators, metric => metric.Label == "Chamados abertos" && !metric.Available);
        Assert.Contains(page.Indicators, metric => metric.Label == "Taxa de conclusão" && metric.Display == "não disponível");
    }

    [Fact]
    public void Customer_success_uses_real_counts_and_names_risk()
    {
        var row = Sample() with { Status = "Blocked", Active7Days = false, Active15Days = false, CompanyDone = false, PlanReviewed = false, HasAdmin = false };
        var page = CustomerSuccessBoard.Build([row], true, true, true);
        Assert.Equal("1", page.Indicators.Single(metric => metric.Label == "Clientes bloqueados").Display);
        Assert.Equal("Risco", page.Clients[0].Health);
    }

    [Fact]
    public void Homologation_scope_hides_other_tenants_and_denies_user()
    {
        Assert.False(HomologationAccess.CanView("User"));
        var client = Guid.NewGuid();
        var scoped = HomologationAccess.Scope("TenantAdmin", client, new HomologationQuery(null, null, "Outro", "Free", "Active", Guid.NewGuid()));
        Assert.Equal(client, scoped.ClientId);
        Assert.Null(HomologationAccess.Scope("SuperAdmin", client, scoped).ClientId);
    }

    [Fact]
    public void Csv_export_escapes_formulas_and_missing_data()
    {
        var csv = HomologationCsv.Write(new HomologationReport([Sample() with { Name = "=cmd", AiKnown = false }], []));
        Assert.Contains("'=cmd", csv);
        Assert.Contains("não disponível", csv);
        Assert.DoesNotContain(Sample().ClientId.ToString(), csv);
    }

    [Fact]
    public void Notification_is_not_created_when_preference_or_condition_blocks_it()
    {
        var alerts = NotificationDispatchPolicy.Evaluate(new AlertSignals(true, null, false, null, null, null, null, null, false, true, true, true, false, false, false));
        var reminder = alerts.Single(alert => alert.Code == "habit_reminder");
        Assert.False(reminder.CanCreateInApp);
        Assert.Null(alerts.Single(alert => alert.Code == "goal_near").Condition);
        Assert.Contains("comercial@mnsoft.com.br", reminder.ChannelNote);
        Assert.Contains("WhatsApp não integrado", reminder.ChannelNote);
    }

    [Fact]
    public void Applied_guide_suggests_template_without_promising_results()
    {
        var knowledge = new AssistantKnowledgeService();
        var answer = knowledge.Guide("sugerir hábito para estudar", null);
        Assert.NotNull(answer);
        Assert.Contains("Estudar 30 minutos", answer!.Message);
        Assert.Contains("não garante", answer.Message);
        Assert.DoesNotContain("resultado garantido", answer.Message, StringComparison.OrdinalIgnoreCase);
        var template = knowledge.Guide("qual template de manhã", null);
        Assert.Contains("Rotina matinal", template!.Message);
        var health = knowledge.Guide("como está a saúde do cliente", null);
        Assert.Contains("/superadmin/customer-success", health!.ActionUrl);
    }

    [Fact]
    public void Screens_explain_onboarding_and_keep_ai_off_fallback()
    {
        var knowledge = new AiKnowledgeBaseService(new AssistantKnowledgeService());
        var screen = knowledge.Explain("/admin/onboarding", "como usar esta tela");
        Assert.Contains("justificativa", screen!.Message);
        Assert.Equal("Knowledge", screen.Provider);
    }

    [Fact]
    public void Routes_and_contrast_are_present()
    {
        var onboarding = File.ReadAllText(Path.Combine(Root, "src/HabitFlow.Web/Views/Admin/Onboarding.cshtml"));
        var css = File.ReadAllText(Path.Combine(Root, "src/HabitFlow.Web/wwwroot/css/product-activation-v6198.css"));
        var layout = File.ReadAllText(Path.Combine(Root, "src/HabitFlow.Web/Views/Shared/_Layout.cshtml"));
        Assert.Contains("admin/onboarding", File.ReadAllText(Path.Combine(Root, "src/HabitFlow.Web/Controllers/ClientOnboardingController.cs")));
        Assert.Contains("[Authorize(Roles = \"SuperAdmin\")]", File.ReadAllText(Path.Combine(Root, "src/HabitFlow.Web/Controllers/SuperAdminController.cs")));
        Assert.Contains("RequireAdmin", File.ReadAllText(Path.Combine(Root, "src/HabitFlow.Web/Controllers/AdminHomologationController.cs")));
        Assert.Contains("product-activation-v6198.css", layout);
        Assert.Contains("#17251f", css);
        Assert.Contains("#fff", css);
        Assert.Contains("Como usar", onboarding);
        Assert.Contains("095_v6198_product_activation.sql", File.ReadAllText(Path.Combine(Root, "database/migrate.sql")));
        Assert.Contains("BEGIN include database/migrations/095_v6198_product_activation.sql", File.ReadAllText(Path.Combine(Root, "database/script_completo.sql")));
    }

    private static ImplantationFacts Facts() => new(false, false, false, false, false, false, false, true, null, false, null, []);

    private static TenantActivationRow Sample() => new(Guid.NewGuid(), "Cliente A", "Active", "Free", "Active", "Approved", "Free", 2, 1, 3, 4, true, 0, true, 1, true, true, true, true, true, true, true, true, true, true, true, true, 1, true, true);
}
