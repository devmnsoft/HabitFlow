using Xunit;
using HabitFlow.Application;

namespace HabitFlow.Tests;

public sealed class ClientRegistrationCpfCnpjV611Tests
{
    private static string Read(string path) => File.ReadAllText(RepositoryRootLocator.PathTo(path));

    [Fact]
    public void DocumentValidator_supports_required_cpf_cnpj_operations()
    {
        var v = new DocumentValidator();
        Assert.Equal("52998224725", v.Normalize("529.982.247-25"));
        Assert.True(v.ValidateCpf("529.982.247-25"));
        Assert.False(v.ValidateCpf("111.111.111-11"));
        Assert.False(v.ValidateCpf("529.982.247-26"));
        Assert.Equal("529.982.247-25", v.FormatCpf("52998224725"));
        Assert.True(v.ValidateCnpj("11.222.333/0001-81"));
        Assert.False(v.ValidateCnpj("00.000.000/0000-00"));
        Assert.False(v.ValidateCnpj("11.222.333/0001-82"));
        Assert.Equal("11.222.333/0001-81", v.FormatCnpj("11222333000181"));
        Assert.Equal("CPF", v.GetDocumentTypeByPersonType("NaturalPerson"));
        Assert.Equal("CNPJ", v.GetDocumentTypeByPersonType("LegalPerson"));
    }

    [Fact]
    public void Public_register_uses_client_registration_service_and_pf_pj_fields()
    {
        var controller = Read("src/HabitFlow.Web/Controllers/AuthController.cs");
        var view = Read("src/HabitFlow.Web/Views/Auth/Register.cshtml");
        var js = Read("src/HabitFlow.Web/wwwroot/js/register-client.js");
        Assert.Contains("ClientAccountRegistrationService", controller);
        Assert.Contains("clientRegistration.RegisterAsync", controller);
        Assert.Contains("Pessoa Física", view);
        Assert.Contains("Pessoa Jurídica", view);
        Assert.Contains("asp-for=\"Document\"", view);
        Assert.Contains("AcceptedTerms", view);
        Assert.Contains("AcceptedPrivacy", view);
        Assert.Contains("maskCpf", js);
        Assert.Contains("maskCnpj", js);
    }

    [Fact]
    public void Registration_service_creates_client_admin_user_free_onboarding_and_duplicate_guard()
    {
        var service = Read("src/HabitFlow.Application/Services/ClientAccountRegistrationService.cs");
        Assert.Contains("DocumentExistsAsync", service);
        Assert.Contains("new Client", service);
        Assert.Contains("UserRole.Admin", service);
        Assert.Contains("UserPlan.Free", service);
        Assert.Contains("ClientPlan.Free", service);
        Assert.Contains("ClientSubscriptionStatus.Free", service);
        Assert.Contains("ClientBenefitsStatus.Free", service);
        Assert.Contains("ClientPaymentStatus.None", service);
        Assert.Contains("onboarding.GetOrCreateAsync", service);
        Assert.Contains("CreateInternalMessageAsync", service);
        Assert.Contains("client.Id", service);
    }

    [Fact]
    public void Migration_030_enforces_document_uniqueness_and_type_coherence()
    {
        var migration = Read("database/migrations/030_client_registration_cpf_cnpj_real_flow.sql");
        var migrate = Read("database/migrate.sql");
        Assert.Contains("habitflow.clients", migration);
        Assert.Contains("ux_habitflow_clients_document_normalized_not_null", migration);
        Assert.Contains("ck_habitflow_clients_person_document_match", migration);
        Assert.Contains("role in ('User','Admin','SuperAdmin')", migration);
        Assert.Contains("030_client_registration_cpf_cnpj_real_flow.sql", migrate);
    }
}
