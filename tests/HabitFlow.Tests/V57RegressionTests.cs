using System.Data;
using HabitFlow.Infrastructure.Data;
using Xunit;

namespace HabitFlow.Tests;

public sealed class DapperDateTimeHandlerTests
{
    [Fact]
    public void DateOnlyTypeHandler_converts_to_database_date()
    {
        var parameter = new FakeParameter();
        new DateOnlyTypeHandler().SetValue(parameter, new DateOnly(2026, 7, 22));
        Assert.Equal(DbType.Date, parameter.DbType);
        Assert.Equal(new DateOnly(2026, 7, 22), parameter.Value);
    }

    [Fact]
    public void TimeOnlyTypeHandler_converts_to_database_time()
    {
        var parameter = new FakeParameter();
        new TimeOnlyTypeHandler().SetValue(parameter, new TimeOnly(8, 30));
        Assert.Equal(DbType.Time, parameter.DbType);
        Assert.Equal(new TimeSpan(8, 30, 0), parameter.Value);
    }

    [Fact]
    public void DapperTypeHandlers_Register_is_idempotent()
    {
        DapperTypeHandlers.Register();
        DapperTypeHandlers.Register();
    }

    private sealed class FakeParameter : IDbDataParameter
    {
        public DbType DbType { get; set; }
        public ParameterDirection Direction { get; set; }
        public bool IsNullable => true;
        public string ParameterName { get; set; } = string.Empty;
        public string SourceColumn { get; set; } = string.Empty;
        public DataRowVersion SourceVersion { get; set; }
        public object? Value { get; set; }
        public byte Precision { get; set; }
        public byte Scale { get; set; }
        public int Size { get; set; }
    }
}

public sealed class PublicPlansAndFaviconTests
{
    [Fact]
    public void Plans_get_is_allow_anonymous_and_billing_controller_stays_authorized()
    {
        var plans = File.ReadAllText(Path.Combine("..", "..", "..", "..", "src", "HabitFlow.Web", "Controllers", "PlansController.cs"));
        var billing = File.ReadAllText(Path.Combine("..", "..", "..", "..", "src", "HabitFlow.Web", "Controllers", "BillingController.cs"));
        Assert.Contains("[AllowAnonymous]", plans, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("[Authorize]\npublic sealed class PlansController", plans, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[Authorize]", billing, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[HttpPost(\"billing/checkout\")]", billing, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Layout_references_svg_favicon_and_file_exists()
    {
        var root = Path.Combine("..", "..", "..", "..");
        Assert.True(File.Exists(Path.Combine(root, "src", "HabitFlow.Web", "wwwroot", "favicon.svg")));
        var layout = File.ReadAllText(Path.Combine(root, "src", "HabitFlow.Web", "Views", "Shared", "_Layout.cshtml"));
        Assert.Contains("/favicon.svg", layout, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class HabitLibraryFallbackTests
{
    [Fact]
    public void Fallback_has_fixed_template_ids_and_productivity_templates()
    {
        var provider = new HabitFlow.Application.HabitLibraryFallbackProvider();
        var templates = provider.GetTemplatesBySlug("produtividade");
        Assert.True(templates.Count >= 5);
        Assert.Contains(templates, t => t.Name == "Planejar o dia");
        Assert.NotEqual(Guid.Empty, templates[0].Id);
    }

    [Fact]
    public void Scripts_include_habit_library_validation()
    {
        var root = Path.Combine("..", "..", "..", "..");
        var script = File.ReadAllText(Path.Combine(root, "database", "script_completo.sql"));
        var validate = File.ReadAllText(Path.Combine(root, "database", "validate_schema_habitflow.sql"));
        Assert.Contains("habitflow.habit_objectives", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("habitflow.habit_templates", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("habit_objectives", validate, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("habit_templates", validate, StringComparison.OrdinalIgnoreCase);
    }
}
