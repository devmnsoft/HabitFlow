using System.Security.Claims;
using HabitFlow.Web.Models;

namespace HabitFlow.Web.Services;

public sealed class HeaderQuickActionService
{
    public IReadOnlyList<HeaderQuickActionViewModel> Build(ClaimsPrincipal user) => user.Identity?.IsAuthenticated != true ? [] :
    [
        new("Novo hábito", "Crie uma nova rotina", "/habits/create", "plus"),
        new("Novo objetivo", "Defina um resultado", "/goals/create", "target"),
        new("Abrir Meu Dia", "Organize o dia", "/my-day", "calendar"),
        new("Revisar progresso", "Acompanhe seu ritmo", "/progress/calendar", "progress"),
        new("Ver relatórios", "Entenda tendências", "/reports", "report"),
        new("Abrir biblioteca", "Use um modelo", "/habit-library", "library")
    ];
}
