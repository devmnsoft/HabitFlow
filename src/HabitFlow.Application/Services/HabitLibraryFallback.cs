using HabitFlow.Domain;

namespace HabitFlow.Application;

public static class HabitLibraryFallback
{
    private static readonly DateTime SeededAt = new(2026, 7, 22, 0, 0, 0, DateTimeKind.Utc);

    public static IReadOnlyList<HabitObjective> Objectives { get; } = new[]
    {
        Objective("11111111-1111-1111-1111-111111111111", "saude", "Saúde", "Cuide do corpo com ações simples todos os dias.", "heart", 1),
        Objective("22222222-2222-2222-2222-222222222222", "estudos", "Estudos", "Crie constância para aprender melhor.", "reading", 2),
        Objective("33333333-3333-3333-3333-333333333333", "produtividade", "Produtividade", "Organize prioridades e conclua tarefas com clareza.", "productivity", 3),
        Objective("44444444-4444-4444-4444-444444444444", "bem-estar", "Bem-estar", "Inclua pausas, presença e autocuidado na rotina.", "wellness", 4),
        Objective("55555555-5555-5555-5555-555555555555", "organizacao", "Organização", "Reduza bagunça e planeje seu dia sem peso.", "organization", 5),
        Objective("66666666-6666-6666-6666-666666666666", "sono", "Sono", "Prepare noites melhores com rituais simples.", "sleep", 6),
        Objective("77777777-7777-7777-7777-777777777777", "exercicio", "Exercício", "Movimente-se com metas pequenas e sustentáveis.", "walk", 7),
        Objective("88888888-8888-8888-8888-888888888888", "leitura", "Leitura", "Transforme poucos minutos em progresso real.", "reading", 8)
    };

    public static IReadOnlyList<HabitTemplate> TemplatesFor(Guid objectiveId, string slug) => Templates(slug, objectiveId);
    public static HabitTemplate? TemplateById(Guid id) => Objectives.SelectMany(o => TemplatesFor(o.Id, o.Slug)).FirstOrDefault(t => t.Id == id);

    private static HabitObjective Objective(string id, string slug, string name, string description, string icon, int sort) => new(Guid.Parse(id), slug, name, description, icon, sort, true, SeededAt);
    private static HabitTemplate T(string id, Guid oid, string n, string d, string c, string color, int min, string b, int s) => HabitLibraryFallbackProvider.Template(id, oid, n, d, c, color, HabitDifficulty.Easy, min, b, s);

    private static HabitTemplate[] Templates(string slug, Guid oid) => slug switch
    {
        "saude" => new[] { T("11111111-0000-0000-0000-000000000001", oid, "Beber água", "Tome água ao iniciar o dia.", "Saúde", "#0EA5E9", 2, "Hidratação consistente.", 1), T("11111111-0000-0000-0000-000000000002", oid, "Comer uma fruta", "Inclua uma fruta na rotina.", "Saúde", "#10B981", 5, "Mais nutrientes.", 2), T("11111111-0000-0000-0000-000000000003", oid, "Alongar por 5 minutos", "Faça alongamentos simples.", "Saúde", "#8B5CF6", 5, "Menos tensão.", 3), T("11111111-0000-0000-0000-000000000004", oid, "Caminhar 20 minutos", "Caminhe em ritmo confortável.", "Exercício", "#22C55E", 20, "Mais energia.", 4), T("11111111-0000-0000-0000-000000000005", oid, "Evitar refrigerante", "Troque refrigerante por água.", "Saúde", "#14B8A6", 1, "Escolhas melhores.", 5) },
        "produtividade" => new[] { T("33333333-0000-0000-0000-000000000001", oid, "Planejar o dia", "Defina suas prioridades.", "Produtividade", "#2563EB", 5, "Mais clareza.", 1), T("33333333-0000-0000-0000-000000000002", oid, "Revisar prioridades", "Revise o foco do dia.", "Produtividade", "#0EA5E9", 5, "Menos dispersão.", 2), T("33333333-0000-0000-0000-000000000003", oid, "Evitar celular por 30 minutos", "Crie um bloco sem distrações.", "Foco", "#6366F1", 30, "Foco profundo.", 3), T("33333333-0000-0000-0000-000000000004", oid, "Finalizar uma pendência", "Conclua uma tarefa pequena.", "Produtividade", "#059669", 15, "Progresso visível.", 4), T("33333333-0000-0000-0000-000000000005", oid, "Organizar tarefas", "Atualize sua lista.", "Organização", "#F59E0B", 10, "Rotina leve.", 5) },
        _ => new[] { T($"99999999-0000-0000-0000-00000000000{1}", oid, "Ler 10 páginas", "Leia sem distrações.", "Leitura", "#F59E0B", 15, "Aprendizado constante.", 1), T($"99999999-0000-0000-0000-00000000000{2}", oid, "Meditar 5 minutos", "Respire com atenção.", "Bem-estar", "#8B5CF6", 5, "Mais presença.", 2), T($"99999999-0000-0000-0000-00000000000{3}", oid, "Revisar agenda", "Veja próximos compromissos.", "Organização", "#10B981", 5, "Menos surpresas.", 3), T($"99999999-0000-0000-0000-00000000000{4}", oid, "Alongar", "Movimente o corpo.", "Exercício", "#22C55E", 5, "Mais disposição.", 4), T($"99999999-0000-0000-0000-00000000000{5}", oid, "Preparar ambiente", "Organize seu espaço.", "Sono", "#0F172A", 5, "Mais tranquilidade.", 5) }
    };
}
