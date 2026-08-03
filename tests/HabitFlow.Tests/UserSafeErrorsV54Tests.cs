using HabitFlow.Application;
using HabitFlow.Web.Models;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace HabitFlow.Tests;

public sealed class UserSafeErrorsV54Tests
{
    [Fact]
    public void RegisterViewModel_compare_is_on_property_and_blocks_mismatch()
    {
        var attr = typeof(RegisterViewModel).GetProperty(nameof(RegisterViewModel.ConfirmPassword))!
            .GetCustomAttributes(typeof(CompareAttribute), inherit: false).SingleOrDefault();
        Assert.NotNull(attr);

        var model = new RegisterViewModel { Name = "Ana", Email = "ana@example.com", Password = "senhasegura", ConfirmPassword = "outra" };
        var results = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(model, new ValidationContext(model), results, true));
        Assert.Contains(results, r => r.ErrorMessage == "As senhas não conferem.");
    }

    [Fact]
    public void Public_error_mapper_hides_postgres_codes()
    {
        var mapper = new UserFacingErrorMapper();
        var message = mapper.ToPublicMessage("postgres.invalid_password");
        Assert.DoesNotContain("postgres.invalid_password", message);
        Assert.DoesNotContain("schemaExists", message);
        Assert.Contains("Não foi possível", message);
    }

    [Fact]
    public void Habit_library_and_home_render_safe_fallback_and_contextual_hero()
    {
        var library = File.ReadAllText(RepositoryRootLocator.PathTo("src/HabitFlow.Web/Views/HabitLibrary/Index.cshtml"));
        var hero = File.ReadAllText(RepositoryRootLocator.PathTo("src/HabitFlow.Web/Views/Shared/Partials/Illustrations/_HeroHabitDashboardIllustration.cshtml"));
        Assert.Contains("explorar hábitos sugeridos", library);
        Assert.Contains("Hoje", hero);
        Assert.Contains("Sequência", hero);
        Assert.Contains("Beber água", hero);
        Assert.DoesNotContain("postgres.invalid_password", library + hero);
    }

    [Fact]
    public void Footer_and_mnsoft_signature_are_compact_and_not_temporary()
    {
        var layout = File.ReadAllText(RepositoryRootLocator.PathTo("src/HabitFlow.Web/Views/Shared/_Layout.cshtml"));
        var signature = File.ReadAllText(RepositoryRootLocator.PathTo("src/HabitFlow.Web/Views/Shared/Partials/_MNSOFTSignatureCompact.cshtml"));
        Assert.Contains("Privacidade", layout);
        Assert.Contains("Termos", layout);
        Assert.Contains("Central de Ajuda", layout);
        Assert.Contains("Consultorias e soluções em TI.", signature);
        Assert.DoesNotContain("temporária", layout + signature, StringComparison.OrdinalIgnoreCase);
    }
}
