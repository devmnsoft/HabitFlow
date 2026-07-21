using HabitFlow.Domain;

namespace HabitFlow.Application;

public static class HabitLibraryFallback
{
    public static IReadOnlyList<HabitObjective> Objectives { get; } = new[]
    {
        Objective("saude", "Saúde", "Cuide do corpo com ações simples todos os dias.", "heart", 1),
        Objective("produtividade", "Produtividade", "Organize prioridades e conclua tarefas com clareza.", "productivity", 2),
        Objective("estudos", "Estudos", "Crie constância para aprender melhor.", "reading", 3),
        Objective("bem-estar", "Bem-estar", "Inclua pausas, presença e autocuidado na rotina.", "wellness", 4),
        Objective("organizacao", "Organização", "Reduza bagunça e planeje seu dia sem peso.", "organization", 5),
        Objective("sono", "Sono", "Prepare noites melhores com rituais simples.", "sleep", 6),
        Objective("exercicio", "Exercício", "Movimente-se com metas pequenas e sustentáveis.", "walk", 7),
        Objective("leitura", "Leitura", "Transforme poucos minutos em progresso real.", "reading", 8)
    };

    public static IReadOnlyList<HabitTemplate> TemplatesFor(Guid objectiveId, string slug) => Templates(slug).Select((h, i) => new HabitTemplate(Guid.CreateVersion7(), objectiveId, h.Name, h.Description, h.Category, "Diário", h.Color, HabitDifficulty.Easy, h.Minutes, h.Benefit, i + 1, true, DateTime.UtcNow, DateTime.UtcNow)).ToArray();

    private static HabitObjective Objective(string slug, string name, string description, string icon, int sort) => new(Guid.CreateVersion7(), slug, name, description, icon, sort, true, DateTime.UtcNow);
    private static IEnumerable<(string Name,string Description,string Category,string Color,int Minutes,string Benefit)> Templates(string slug) => slug switch
    {
        "saude" => new[]{ H("Beber água", "Tome um copo de água ao iniciar o dia.", "Saúde", "#0EA5E9", 2, "Hidratação consistente."), H("Caminhada curta", "Caminhe por 10 minutos.", "Exercício", "#10B981", 10, "Mais energia."), H("Alongar", "Faça uma pausa rápida para alongar.", "Bem-estar", "#8B5CF6", 5, "Menos tensão.")},
        "sono" => new[]{ H("Desligar telas", "Evite telas 30 minutos antes de dormir.", "Sono", "#6366F1", 5, "Sono mais leve."), H("Preparar quarto", "Deixe o ambiente escuro e confortável.", "Sono", "#0F172A", 5, "Ritual de descanso."), H("Horário fixo", "Deite em um horário consistente.", "Sono", "#7C3AED", 1, "Mais regularidade.")},
        _ => new[]{ H("Planejar o dia", "Escolha uma prioridade antes de começar.", "Organização", "#059669", 5, "Mais clareza."), H("Ler 10 páginas", "Leia um trecho curto sem distrações.", "Leitura", "#F59E0B", 15, "Aprendizado constante."), H("Revisar progresso", "Marque o que foi concluído hoje.", "Produtividade", "#2563EB", 3, "Consistência visível.")}
    };
    private static (string,string,string,string,int,string) H(string n,string d,string c,string color,int m,string b)=>(n,d,c,color,m,b);
}
