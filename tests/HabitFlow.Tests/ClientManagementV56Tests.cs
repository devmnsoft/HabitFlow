using HabitFlow.Application;
using HabitFlow.Domain;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace HabitFlow.Tests;

public sealed class ClientManagementV56Tests
{
    private static string Read(string path) => File.ReadAllText(Path.Combine(GetRoot(), path));
    private static string GetRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "README.md"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Repo root not found.");
    }

    [Fact]
    public void ClientRequestsUseNonSealedBase()
    {
        Assert.False(typeof(ClientRequestBase).IsSealed);
        Assert.True(typeof(CreateClientRequest).IsSealed);
        Assert.True(typeof(UpdateClientRequest).IsSealed);
        Assert.Equal(typeof(ClientRequestBase), typeof(CreateClientRequest).BaseType);
        Assert.Equal(typeof(ClientRequestBase), typeof(UpdateClientRequest).BaseType);
    }

    [Fact]
    public void ClientRequestBaseValidatesCommonFields()
    {
        var request = new CreateClientRequest { Name = string.Empty, Email = "email-invalido", Document = "12", Phone = new string('9', 41), Plan = ClientPlan.Free, Status = ClientStatus.Active };
        var results = new List<ValidationResult>();

        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);

        Assert.Contains(results, r => r.ErrorMessage == "Informe o nome do cliente.");
        Assert.Contains(results, r => r.ErrorMessage == "Informe um e-mail válido.");
        Assert.Contains(results, r => r.ErrorMessage == "Informe um documento válido.");
        Assert.Contains(results, r => r.ErrorMessage == "O telefone deve ter no máximo 40 caracteres.");
    }

    [Fact]
    public void AdminClientsControllerUsesCorrectViewModelsAndMessages()
    {
        var controller = Read("src/HabitFlow.Web/Controllers/AdminClientsController.cs");
        Assert.Contains("new CreateClientRequest()", controller);
        Assert.Contains("new UpdateClientRequest", controller);
        Assert.Contains("Index([FromQuery] ClientFilter filter", controller);
        Assert.Contains("O cliente foi cadastrado com sucesso.", controller);
        Assert.Contains("As informações do cliente foram salvas.", controller);
        Assert.Contains("O cliente voltou a ficar ativo.", controller);
        Assert.Contains("O cliente foi desativado, mas o histórico foi mantido.", controller);
        Assert.Contains("O cliente foi bloqueado com segurança.", controller);
        Assert.Contains("Tente novamente em instantes ou verifique a configuração do banco.", controller);
    }

    [Fact]
    public void ClientServiceUsesBaseValidationAndAuditsMutations()
    {
        var service = Read("src/HabitFlow.Application/Services/ClientService.cs");
        Assert.Contains("ValidateAsync(ClientRequestBase", service);
        Assert.Contains("CreateAsync(CreateClientRequest", service);
        Assert.Contains("UpdateAsync(Guid id, UpdateClientRequest", service);
        Assert.Contains("audit.LogAsync(\"client_created\"", service);
        Assert.Contains("audit.LogAsync(\"client_updated\"", service);
        Assert.Contains("audit.LogAsync(action", service);
        Assert.Contains("logger.LogError", service);
        Assert.Contains("Result<Client>.Failure(\"database\"", service);
    }

    [Fact]
    public void ClientViewsDeclareExistingModels()
    {
        Assert.Contains("@model HabitFlow.Application.CreateClientRequest", Read("src/HabitFlow.Web/Views/Admin/Clients/Create.cshtml"));
        Assert.Contains("@model HabitFlow.Application.UpdateClientRequest", Read("src/HabitFlow.Web/Views/Admin/Clients/Edit.cshtml"));
        Assert.Contains("@model IReadOnlyList<HabitFlow.Application.ClientListItemDto>", Read("src/HabitFlow.Web/Views/Admin/Clients/Index.cshtml"));
        Assert.Contains("@model HabitFlow.Application.ClientDetailDto", Read("src/HabitFlow.Web/Views/Admin/Clients/Details.cshtml"));
    }

    [Fact]
    public void FeedbackServiceDefinesStructuredTempData()
    {
        var feedback = Read("src/HabitFlow.Web/Services/ApplicationFeedbackService.cs");
        Assert.Contains("SetSuccess", feedback);
        Assert.Contains("SetInfo", feedback);
        Assert.Contains("SetWarning", feedback);
        Assert.Contains("SetError", feedback);
        Assert.Contains("SetDatabaseError", feedback);
        Assert.Contains("SetFeedback", feedback);
        Assert.Contains("TempData[\"Feedback.Type\"]", feedback);
        Assert.Contains("TempData[\"Feedback.Title\"]", feedback);
        Assert.Contains("TempData[\"Feedback.Message\"]", feedback);
    }

    [Fact]
    public void ClientRepositoryUsesHabitflowSchema()
    {
        var sql = Read("src/HabitFlow.Infrastructure/Repositories/ClientRepository.cs");
        Assert.Contains("from habitflow.clients", sql);
        Assert.Contains("insert into habitflow.clients", sql);
        Assert.Contains("update habitflow.clients", sql);
    }

    [Fact]
    public void AdminClientsControllerRequiresAdminAndFeedback()
    {
        var controller = Read("src/HabitFlow.Web/Controllers/AdminClientsController.cs");
        Assert.Contains("[Authorize(Roles = \"Admin\")]", controller);
        Assert.Contains("[ValidateAntiForgeryToken]", controller);
        Assert.Contains("O cliente foi cadastrado com sucesso.", controller);
        Assert.Contains("SetDatabaseError", controller);
    }

    [Fact]
    public void FeedbackBridgeAndMnsoftBadgeAreSafe()
    {
        var bridge = Read("src/HabitFlow.Web/Views/Shared/Partials/_FeedbackBridge.cshtml");
        var badge = Read("src/HabitFlow.Web/Views/Shared/Partials/_MNSOFTBrandBadge.cshtml");
        Assert.Contains("JsonSerializer.Serialize", bridge);
        Assert.DoesNotContain("assinatura temporária", badge, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Consultorias e soluções em TI.", badge);
    }

    [Fact]
    public void ClientMigrationCreatesExpectedSchemaObjects()
    {
        var migration = Read("database/migrations/021_clients_management.sql");
        Assert.Contains("create table if not exists habitflow.clients", migration);
        Assert.Contains("ix_habitflow_clients_name", migration);
        Assert.Contains("check (status in ('Active', 'Inactive', 'Blocked'))", migration);
    }
}
