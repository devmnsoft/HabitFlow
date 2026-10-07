using HabitFlow.Application;
using HabitFlow.Domain;
using HabitFlow.Web.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HabitFlow.Tests;

public sealed class PlansAndAiAdminV6197Tests
{
    private static readonly string Root = RepositoryRootLocator.Root;
    private static string Read(params string[] parts) => File.ReadAllText(Path.Combine(Root, Path.Combine(parts)));

    [Fact]
    public void Paid_plans_keep_limits_blocks_and_manual_support_without_a_fake_charge()
    {
        Assert.Equal("Não informado", PlanLandingPageService.FormatLimit(null));
        Assert.Equal("Ilimitado", PlanLandingPageService.FormatLimit(-1));
        Assert.Equal("5", PlanLandingPageService.FormatLimit(5));

        var service = Read("src", "HabitFlow.Web", "Services", "PlanLandingPageService.cs");
        var card = Read("src", "HabitFlow.Web", "Views", "Plans", "Partials", "_CommercialPlanCard.cshtml");
        var plans = Read("src", "HabitFlow.Web", "Controllers", "PlansController.cs");
        var billing = Read("src", "HabitFlow.Web", "Views", "Billing", "Index.cshtml");
        var checkout = Read("src", "HabitFlow.Web", "Controllers", "BillingController.cs");
        Assert.Contains("\"Premium\"", service);
        Assert.Contains("\"Free\"", service);
        Assert.Contains("PlanCodes.Team", service);
        Assert.Contains("PlanCodes.Enterprise", service);
        Assert.Contains("comercial@mnsoft.com.br", service);
        Assert.Contains("Assinar @Model.Name", card);
        Assert.Contains("Ver pagamento pendente", card);
        Assert.Contains("Falar com a MNSOFT", card);
        Assert.DoesNotContain("checkout?success=true", card, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("subscription.PlanCode", plans);
        Assert.Contains("Os recursos pagos ainda não foram liberados", plans);
        Assert.Contains("Checkout online indisponível", billing);
        Assert.Contains("Nenhuma cobrança é simulada", billing);
        Assert.Contains("Pagamento pendente", billing);
        Assert.Contains("Você está no plano Free", billing);
        Assert.Contains("PlanCodes.Enterprise", checkout);
        Assert.DoesNotContain("fake", checkout, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Ai_admin_is_split_between_superadmin_and_the_current_tenant()
    {
        var super = Read("src", "HabitFlow.Web", "Controllers", "SuperAdminAiController.cs");
        var tenant = Read("src", "HabitFlow.Web", "Controllers", "TenantAiController.cs");
        var superView = Read("src", "HabitFlow.Web", "Views", "SuperAdminAi", "Index.cshtml");
        var tenantView = Read("src", "HabitFlow.Web", "Views", "TenantAi", "Index.cshtml");
        var migration = Read("database", "migrations", "094_v6197_ai_admin_usage.sql");
        var migrate = Read("database", "migrate.sql");
        var complete = Read("database", "script_completo.sql");
        var events = Read("src", "HabitFlow.Application", "Observability", "ApplicationEvents.cs");
        Assert.Contains("[Authorize(Roles = \"SuperAdmin\")]", super);
        Assert.Contains("Chaves continuam só nas variáveis de ambiente", super);
        Assert.Contains("[Authorize(Roles = \"Admin,TenantAdmin,TenantOwner,SuperAdmin\")]", tenant);
        Assert.Contains("CurrentClientId()", tenant);
        Assert.DoesNotContain("ApiKey", superView);
        Assert.DoesNotContain("ApiKey", tenantView);
        Assert.Contains("Chaves, tokens e a configuração global não aparecem", tenantView);
        Assert.Contains("Limite global diário", superView);
        Assert.Contains("Consumo por tenant hoje", superView);
        Assert.Contains("Falhas recentes", superView);
        Assert.DoesNotContain("api_key", migration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("prompt varchar", migration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("message_text", migration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ai_usage_events", migration);
        Assert.Contains("ai_provider_events", migration);
        Assert.Contains("ai_safety_events", migration);
        Assert.Contains("ai_tenant_limits", migration);
        Assert.Contains("ix_ai_usage_events_tenant_date", migration);
        Assert.Contains("\\i database/migrations/094_v6197_ai_admin_usage.sql", migrate);
        Assert.Contains("-- BEGIN include database/migrations/094_v6197_ai_admin_usage.sql", complete);
        foreach (var name in new[] { "ai.chat.opened", "ai.request.started", "ai.request.completed", "ai.request.failed", "ai.request.blocked_by_plan", "ai.request.blocked_by_guardrail", "ai.provider.unavailable", "ai.provider.timeout", "ai.settings.updated", "design.validation.failed", "ui.form.validation.failed" })
            Assert.Contains(name, events);
    }

    [Fact]
    public async Task Admin_save_rejects_an_unknown_provider_and_keeps_limits_inside_the_plan()
    {
        var repository = new CountingRepository();
        var admin = new AiAdminService(repository, new AiUsageLimiter(), NullLogger<AiAdminService>.Instance);
        var invalid = await admin.SaveAsync(Settings("Outro"), [new("free", 5)], null, CancellationToken.None);
        Assert.Equal("O formato informado não é válido.", invalid);
        Assert.Equal(0, repository.Saves);

        var saved = await admin.SaveAsync(Settings("Groq"), [new("free", 5), new("ritmo", 40), new("team", 40), new("enterprise", 80)], Guid.NewGuid(), CancellationToken.None);
        Assert.Null(saved);
        Assert.Equal(1, repository.Saves);

        var client = Guid.NewGuid();
        var user = Guid.NewGuid();
        Assert.True(await admin.AllowAsync(client, user, UserPlan.Free, CancellationToken.None));
        repository.Used = 5;
        Assert.False(await admin.AllowAsync(client, user, UserPlan.Free, CancellationToken.None));
        await admin.RecordAsync(client, user, "Groq", "llama", "BlockedByPlan", "ai.request.blocked_by_plan", "corr-1", 0, CancellationToken.None);
        Assert.Equal("corr-1", repository.LastCorrelation);
        Assert.Equal(client, repository.LastClient);
    }

    private static AiRuntimeSettings Settings(string provider) => new(false, provider, false, false, false, "", "", "", 200, DateTime.UtcNow);

    private sealed class CountingRepository : IAiAdminRepository
    {
        public int Saves { get; private set; }
        public int Used { get; set; }
        public string? LastCorrelation { get; private set; }
        public Guid LastClient { get; private set; }

        public Task<AiRuntimeSettings> GetSettingsAsync(CancellationToken ct = default) => Task.FromResult(Settings(""));
        public Task SaveSettingsAsync(AiRuntimeSettings settings, Guid? userId, CancellationToken ct = default) { Saves++; return Task.CompletedTask; }
        public Task<IReadOnlyList<AiPlanLimit>> ListPlanLimitsAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<AiPlanLimit>>([]);
        public Task SavePlanLimitAsync(string planCode, int dailyLimit, CancellationToken ct = default) => Task.CompletedTask;
        public Task<int> GetEffectiveDailyLimitAsync(Guid clientId, CancellationToken ct = default) => Task.FromResult(5);
        public Task<int> CountUserTodayAsync(Guid clientId, Guid userId, CancellationToken ct = default) => Task.FromResult(Used);
        public Task<int> CountGlobalTodayAsync(CancellationToken ct = default) => Task.FromResult(0);
        public Task RecordAsync(Guid clientId, Guid userId, string provider, string model, string status, string eventCode, string correlationId, int durationMs, CancellationToken ct = default)
        {
            LastClient = clientId;
            LastCorrelation = correlationId;
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<AiUsageEventRow>> RecentAsync(Guid? clientId, int take, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<AiUsageEventRow>>([]);
        public Task<IReadOnlyList<AiUsageEventRow>> RecentFailuresAsync(Guid? clientId, int take, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<AiUsageEventRow>>([]);
        public Task<IReadOnlyList<AiTenantConsumption>> ConsumptionAsync(Guid? clientId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<AiTenantConsumption>>([]);
    }
}
