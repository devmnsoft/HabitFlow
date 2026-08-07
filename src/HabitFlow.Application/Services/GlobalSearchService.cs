using HabitFlow.Domain;

namespace HabitFlow.Application;

public sealed class GlobalSearchService(
    IHabitRepository habits,
    IUserGoalRepository goals,
    HabitLibraryFallbackProvider library)
{
    private static readonly GlobalSearchResult[] Destinations =
    [
        new("Página", "Meu dia", "Organize e conclua a rotina de hoje.", "/my-day", "sun"),
        new("Página", "Hábitos", "Crie e acompanhe seus hábitos.", "/habits", "check"),
        new("Página", "Objetivos", "Conecte hábitos ao que importa.", "/goals", "target"),
        new("Relatório", "Progresso", "Veja seu calendário de consistência.", "/progress/calendar", "chart"),
        new("Relatório", "Relatórios", "Analise semanas e meses com dados reais.", "/reports", "chart"),
        new("Página", "Biblioteca", "Encontre hábitos prontos para começar.", "/habit-library", "book"),
        new("Configuração", "Meu plano", "Consulte uso, limites e recursos.", "/account/plan/usage", "sparkle"),
        new("Configuração", "Segurança", "Proteja sua conta e suas sessões.", "/account/security", "shield"),
        new("Configuração", "Privacidade", "Gerencie seus dados e consentimentos.", "/account/privacy", "lock")
    ];

    public async Task<IReadOnlyList<GlobalSearchResult>> SearchAsync(
        Guid clientId, Guid userId, string? query, int limit = 12, CancellationToken ct = default)
    {
        var term = query?.Trim();
        if (clientId == Guid.Empty || userId == Guid.Empty || string.IsNullOrWhiteSpace(term) || term.Length < 2)
            return [];

        limit = Math.Clamp(limit, 1, 25);
        var comparison = StringComparison.CurrentCultureIgnoreCase;
        bool Matches(params string?[] values) => values.Any(value => value?.Contains(term, comparison) == true);

        // Repository methods enforce tenant ownership. Never use the legacy id-only lookup here.
        var ownedHabits = await habits.ListActiveAsync(clientId, userId, ct);
        var ownedGoals = await goals.ListAsync(clientId, userId, ct);

        var results = new List<GlobalSearchResult>();
        results.AddRange(ownedHabits
            .Where(h => Matches(h.Name, h.Notes, h.Category))
            .Select(h => new GlobalSearchResult("Hábito", h.Name, h.Notes ?? "Abrir detalhes do hábito.", $"/habits/{h.Id}", "check")));
        results.AddRange(ownedGoals
            .Where(g => Matches(g.Title, g.Description))
            .Select(g => new GlobalSearchResult("Objetivo", g.Title, g.Description ?? "Abrir detalhes do objetivo.", $"/goals/{g.Id}", "target")));
        results.AddRange(library.GetObjectives().SelectMany(objective => library.GetTemplatesBySlug(objective.Slug))
            .Where(template => template.IsActive && Matches(template.Name, template.Description, template.Category, template.Tags is null ? null : string.Join(' ', template.Tags)))
            .Select(template => new GlobalSearchResult("Biblioteca", template.Name, template.Description, $"/habit-library/{template.Id}", "book")));
        results.AddRange(Destinations.Where(item => Matches(item.Title, item.Description, item.Type)));

        return results
            .GroupBy(item => (item.Type, item.Url))
            .Select(group => group.First())
            .OrderBy(item => item.Title.StartsWith(term, comparison) ? 0 : 1)
            .ThenBy(item => item.Type)
            .ThenBy(item => item.Title)
            .Take(limit)
            .ToList();
    }
}
