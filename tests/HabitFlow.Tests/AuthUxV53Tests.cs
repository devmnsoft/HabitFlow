using HabitFlow.Application;
using HabitFlow.Web.Models;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace HabitFlow.Tests;

public sealed class AuthUxV53Tests
{
    [Fact]
    public void RegisterViewModel_requires_confirm_password_and_matching_passwords()
    {
        var dto = new RegisterViewModel { Name = "Ana", Email = "ana@example.com", Password = "senhasegura", ConfirmPassword = "diferente" };
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.Contains(results, r => r.ErrorMessage == "As senhas não conferem.");
    }

    [Fact]
    public void PostgresErrorHelper_returns_public_and_development_messages_for_28P01()
    {
        var ex = new Exception("falha");
        ex.Data["SqlState"] = PostgresErrorHelper.InvalidPasswordSqlState;
        Assert.Equal("Não foi possível acessar o banco de dados com as credenciais configuradas.", PostgresErrorHelper.ToPublicUserMessage(ex, false));
        Assert.Equal("A senha do PostgreSQL está incorreta. Revise Username e Password em appsettings.Development.local.json.", PostgresErrorHelper.ToDeveloperHint(ex));
        Assert.Equal(PostgresErrorHelper.InvalidPasswordCode, PostgresErrorHelper.BuildErrorCode(ex));
    }

    [Fact]
    public void Views_and_assets_include_password_toggles_messages_and_no_binary_references()
    {
        var register = File.ReadAllText(RepositoryRootLocator.PathTo("src/HabitFlow.Web/Views/Auth/Register.cshtml"));
        var login = File.ReadAllText(RepositoryRootLocator.PathTo("src/HabitFlow.Web/Views/Auth/Login.cshtml"));
        var passwordPartial = File.ReadAllText(RepositoryRootLocator.PathTo("src/HabitFlow.Web/Views/Shared/Partials/_PasswordInput.cshtml"));
        var messagesPartial = File.ReadAllText(RepositoryRootLocator.PathTo("src/HabitFlow.Web/Views/Shared/Partials/_AppMessages.cshtml"));
        var js = File.ReadAllText(RepositoryRootLocator.PathTo("src/HabitFlow.Web/wwwroot/js/site.js"));
        Assert.Contains("ConfirmPassword", register);
        Assert.Contains("data-password-toggle", register + login + passwordPartial);
        Assert.Contains("type=\"password\"", passwordPartial);
        Assert.Contains("initPasswordToggles", js);
        Assert.Contains("hf-message", messagesPartial);
        Assert.DoesNotContain("Assinatura visual temporária", register + login);
        Assert.DoesNotContain("base64", register + login);
    }
}
