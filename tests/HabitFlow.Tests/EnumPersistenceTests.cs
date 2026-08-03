using HabitFlow.Application;
using HabitFlow.Domain;
using HabitFlow.Infrastructure;
using Xunit;

namespace HabitFlow.Tests;

public sealed class EnumPersistenceTests
{
    [Fact]
    public void DbEnum_converts_database_enums_to_constraint_text()
    {
        Assert.Equal("Active", DbEnum.Text(AccountStatus.Active));
        Assert.Equal("User", DbEnum.Text(UserRole.User));
        Assert.Equal("Info", DbEnum.Text(AuditSeverity.Info));
        Assert.Equal("Error", DbEnum.Text(AuditSeverity.Error));
        Assert.Equal("Free", DbEnum.Text(UserPlan.Free));
        Assert.Equal("Active", DbEnum.Text(PlanStatus.Active));
        Assert.Equal("Active", DbEnum.Text(ClientStatus.Active));
        Assert.Equal("Free", DbEnum.Text(ClientPlan.Free));
    }

    [Fact]
    public void Repositories_do_not_cast_enum_parameters_with_postgres_text_casts()
    {
        var userRepo = File.ReadAllText(RepositoryRootLocator.PathTo("src/HabitFlow.Infrastructure/Repositories/UserRepository.cs"));
        var auditRepo = File.ReadAllText(RepositoryRootLocator.PathTo("src/HabitFlow.Infrastructure/Repositories/AuditRepository.cs"));
        Assert.Contains("Role = DbEnum.Text(u.Role)", userRepo);
        Assert.Contains("AccountStatus = DbEnum.Text(u.AccountStatus)", userRepo);
        Assert.Contains("Plan = DbEnum.Text(u.Plan)", userRepo);
        Assert.Contains("Severity = DbEnum.Text(log.Severity)", auditRepo);
        Assert.DoesNotContain("@AccountStatus::text", userRepo);
        Assert.DoesNotContain("@Severity::text", auditRepo);
    }

    [Fact]
    public void PostgresErrorHelper_recognizes_check_constraint_violation_without_leaking_constraint_name()
    {
        var ex = new Exception("ck_habitflow_users_account_status");
        ex.Data["SqlState"] = PostgresErrorHelper.CheckConstraintViolationSqlState;
        Assert.True(PostgresErrorHelper.IsCheckConstraintViolation(ex));
        Assert.Equal(PostgresErrorHelper.CheckConstraintViolationCode, PostgresErrorHelper.BuildErrorCode(ex));
        var publicMessage = PostgresErrorHelper.ToPublicUserMessage(ex, false);
        Assert.Equal(PostgresErrorHelper.FriendlyCheckConstraintViolationMessage, publicMessage);
        Assert.DoesNotContain("ck_habitflow_users_account_status", publicMessage);
    }

    [Fact]
    public void Public_navigation_footer_and_help_icon_are_safe()
    {
        var layout = File.ReadAllText(RepositoryRootLocator.PathTo("src/HabitFlow.Web/Views/Shared/_Layout.cshtml"));
        var icons = File.ReadAllText(RepositoryRootLocator.PathTo("src/HabitFlow.Web/Views/Shared/Partials/Icons/_Icon.cshtml"));
        var footerBadge = File.ReadAllText(RepositoryRootLocator.PathTo("src/HabitFlow.Web/Views/Shared/Partials/_MNSOFTBrandBadge.cshtml"));
        Assert.Contains("Planos", layout);
        Assert.DoesNotContain("Assinatura visual temporária", layout + footerBadge);
        Assert.Contains("CNPJ 18.160.057/0001-13", layout);
        Assert.Contains("comercial@mnsoft.com.br", layout);
        Assert.Contains("logo-mnsoft-oficial.png", footerBadge);
        Assert.DoesNotContain("¿", icons + layout);
        Assert.Contains("M12 21a9 9", icons);
    }
}
