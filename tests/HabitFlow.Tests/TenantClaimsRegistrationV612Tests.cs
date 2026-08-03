using HabitFlow.Application;
using Xunit;

namespace HabitFlow.Tests;

public sealed class TenantClaimsRegistrationV612Tests
{
    private static string Read(string path) => File.ReadAllText(RepositoryRootLocator.PathTo(path));

    [Fact]
    public void Login_adds_client_id_claim_when_user_has_client()
    {
        var controller = Read("src/HabitFlow.Web/Controllers/AuthController.cs");
        Assert.Contains("user.ClientId.HasValue", controller);
        Assert.Contains("new Claim(\"client_id\", user.ClientId.Value.ToString())", controller);
    }

    [Fact]
    public void Current_user_context_exposes_tenant_helpers()
    {
        var context = Read("src/HabitFlow.Application/Services/CurrentUserContext.cs");
        Assert.Contains("FindFirstValue(\"client_id\")", context);
        Assert.Contains("RequiresClient", context);
        Assert.Contains("HasClient", context);
        Assert.Contains("Role == UserRole.SuperAdmin", context);
    }

    [Fact]
    public void Registration_service_uses_unit_of_work_transaction()
    {
        var service = Read("src/HabitFlow.Application/Services/ClientAccountRegistrationService.cs");
        Assert.Contains("BeginTransactionAsync", service);
        Assert.Contains("CommitAsync", service);
        Assert.Contains("RollbackAsync", service);
        Assert.True(service.IndexOf("clients.CreateAsync", StringComparison.Ordinal) < service.IndexOf("users.CreateAsync", StringComparison.Ordinal));
    }

    [Fact]
    public void Middleware_blocks_admin_user_without_client_id_with_friendly_recovery()
    {
        var middleware = Read("src/HabitFlow.Web/Middleware/ClientBindingMiddleware.cs");
        Assert.Contains("client_id", middleware);
        Assert.Contains("/admin/onboarding/recover-client", middleware);
        var view = Read("src/HabitFlow.Web/Views/Admin/RecoverClient.cshtml");
        Assert.Contains("Seu usuário ainda não está vinculado a uma conta", view);
    }

    [Fact]
    public void Superadmin_registration_quality_and_database_migration_exist()
    {
        var controller = Read("src/HabitFlow.Web/Controllers/SuperAdminController.cs");
        var view = Read("src/HabitFlow.Web/Views/SuperAdmin/Registrations/Index.cshtml");
        var migration = Read("database/migrations/031_registration_claims_onboarding_quality.sql");
        Assert.Contains("registrations", controller);
        Assert.Contains("export/registrations", controller);
        Assert.Contains("Cadastros recentes", view);
        Assert.Contains("vw_client_registration_quality", migration);
        Assert.Contains("ix_habitflow_users_client_id", migration);
    }

    [Theory]
    [InlineData("529.982.247-25", true)]
    [InlineData("111.111.111-11", false)]
    [InlineData("11.222.333/0001-81", true)]
    [InlineData("00.000.000/0000-00", false)]
    public void Document_validator_keeps_real_cpf_cnpj_rules(string document, bool expected)
    {
        var validator = new DocumentValidator();
        var normalized = validator.Normalize(document);
        var actual = normalized.Length == 11 ? validator.ValidateCpf(document) : validator.ValidateCnpj(document);
        Assert.Equal(expected, actual);
    }
}
