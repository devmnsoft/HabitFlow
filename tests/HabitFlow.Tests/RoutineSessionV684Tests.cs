using Xunit;

namespace HabitFlow.Tests;

public sealed class RoutineSessionV684Tests
{
    [Fact]
    public void Migration_Persists_Named_Sessions_With_Continuity_And_Idempotency_Contracts()
    {
        var migration = File.ReadAllText(RepositoryRootLocator.PathTo("database", "migrations", "107_routine_sessions.sql"));
        var script = File.ReadAllText(RepositoryRootLocator.PathTo("database", "script_completo.sql"));

        Assert.Contains("create table if not exists habitflow.routine_sessions", migration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("create table if not exists habitflow.routine_session_items", migration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("create table if not exists habitflow.routine_session_transitions", migration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("status in ('Prepared','InProgress','Paused','Closed','Canceled')", migration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ux_routine_sessions_one_active", migration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("where status in ('Prepared','InProgress','Paused')", migration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ux_routine_session_transition_idempotency", migration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("active_account_session_id", migration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("create table if not exists habitflow.routine_sessions", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("END include database/migrations/107_routine_sessions.sql", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RoutineSession_Service_Reuses_Canonical_Completion_And_Does_Not_Auto_Complete_On_Start()
    {
        var service = File.ReadAllText(RepositoryRootLocator.PathTo("src", "HabitFlow.Application", "Services", "RoutineSessionService.cs"));

        Assert.Contains("CompleteHabitUseCase completeHabit", service, StringComparison.Ordinal);
        Assert.Contains("completeHabit.ExecuteAsync", service, StringComparison.Ordinal);
        Assert.Contains("Iniciar não conclui hábitos automaticamente", service, StringComparison.Ordinal);
        Assert.Contains("Versão mínima registrada sem marcar alvo completo", service, StringComparison.Ordinal);
        Assert.Contains("Já existe uma sessão ativa", service, StringComparison.Ordinal);
        Assert.Contains("Item pulado nesta sessão; a ocorrência continua existindo na agenda", service, StringComparison.Ordinal);
        Assert.Contains("Comando repetido; estado preservado pelo servidor", service, StringComparison.Ordinal);
        Assert.DoesNotContain("AddAsync(new HabitCompletion", service, StringComparison.Ordinal);
    }

    [Fact]
    public void RoutineSession_Routes_Are_Private_And_Navigable()
    {
        var controller = File.ReadAllText(RepositoryRootLocator.PathTo("src", "HabitFlow.Web", "Controllers", "RoutineSessionsController.cs"));
        var navigation = File.ReadAllText(RepositoryRootLocator.PathTo("src", "HabitFlow.Web", "Services", "NavigationService.cs"));
        var middleware = File.ReadAllText(RepositoryRootLocator.PathTo("src", "HabitFlow.Web", "Middleware", "AccountStatusMiddleware.cs"));
        var serviceWorker = File.ReadAllText(RepositoryRootLocator.PathTo("src", "HabitFlow.Web", "wwwroot", "service-worker.js"));

        Assert.Contains("[Authorize]", controller, StringComparison.Ordinal);
        Assert.Contains("[Route(\"routine-sessions\")]", controller, StringComparison.Ordinal);
        Assert.Contains("Code: \"routine-sessions\"", navigation, StringComparison.Ordinal);
        Assert.Contains("/routine-sessions", middleware, StringComparison.Ordinal);
        Assert.Contains("routine-sessions", serviceWorker, StringComparison.Ordinal);
    }

    [Fact]
    public void RoutineSession_Views_Surface_Preparation_Resume_History_And_Private_Reflection()
    {
        var index = File.ReadAllText(RepositoryRootLocator.PathTo("src", "HabitFlow.Web", "Views", "RoutineSessions", "Index.cshtml"));
        var show = File.ReadAllText(RepositoryRootLocator.PathTo("src", "HabitFlow.Web", "Views", "RoutineSessions", "Show.cshtml"));
        var history = File.ReadAllText(RepositoryRootLocator.PathTo("src", "HabitFlow.Web", "Views", "RoutineSessions", "History.cshtml"));

        Assert.Contains("Tempo disponível", index, StringComparison.Ordinal);
        Assert.Contains("Retomar", index, StringComparison.Ordinal);
        Assert.Contains("Mínimo", show, StringComparison.Ordinal);
        Assert.Contains("Alvo completo", show, StringComparison.Ordinal);
        Assert.Contains("Reflexão privada opcional", show, StringComparison.Ordinal);
        Assert.Contains("Sessões canceladas permanecem visíveis", history, StringComparison.Ordinal);
    }
}
