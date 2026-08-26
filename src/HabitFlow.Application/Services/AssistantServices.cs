using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using HabitFlow.Domain;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed record AssistantRequest(string Message, Guid ClientId, Guid UserId, string CorrelationId);
public sealed record AssistantResponse(string Message, string Provider, string SafetyStatus, string? ActionUrl = null, string? ActionLabel = null);
public sealed record AssistantUserContext(int ActiveHabits, int PausedHabits, string? MostConsistentHabit, UserPlan Plan, int Reminders);
public interface IAssistantProvider { bool IsConfigured { get; } Task<AssistantResponse> GenerateAsync(AssistantRequest request, AssistantUserContext context, CancellationToken ct); }

public sealed class AssistantSafetyPolicy
{
    private static readonly Regex SecretPattern = new(@"(?i)(password|senha|token|secret|connection\s*string|cookie|authorization)\s*[:=]", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
    private static readonly string[] Destructive = ["excluir", "apagar hábito", "cancelar assinatura", "alterar senha", "alterar e-mail", "cartão", "pagamento"];
    private static readonly string[] OutOfScope = ["diagnóstico médico", "remédio", "processo judicial", "investimento", "ação na bolsa"];
    private static readonly string[] PromptInjection = ["ignore as instruções", "ignore previous", "prompt do sistema", "system prompt", "modo desenvolvedor", "jailbreak", "revele os dados", "outro tenant"];
    public bool ContainsSensitiveData(string value) => SecretPattern.IsMatch(value ?? string.Empty);
    public bool IsDestructive(string value) => Destructive.Any(x => value.Contains(x, StringComparison.OrdinalIgnoreCase));
    public bool IsOutOfScope(string value) => OutOfScope.Any(x => value.Contains(x, StringComparison.OrdinalIgnoreCase));
    public bool IsPromptInjection(string value) => PromptInjection.Any(x => value.Contains(x, StringComparison.OrdinalIgnoreCase));
    public string Sanitize(string value)
    {
        var clean = Regex.Replace(value ?? string.Empty, @"(?i)(bearer\s+)[A-Za-z0-9._~-]+", "$1[REMOVIDO]", RegexOptions.None, TimeSpan.FromMilliseconds(100));
        clean = Regex.Replace(clean, @"(?i)(password|senha|token|secret|cookie)\s*[:=]\s*\S+", "$1=[REMOVIDO]", RegexOptions.None, TimeSpan.FromMilliseconds(100));
        return clean.Length <= 500 ? clean : clean[..500];
    }
}

public sealed record HelpArticle(string Title, string Slug, string Category, string Question, string Answer, string[] Tags, bool Active, int Order, DateTime UpdatedAt);
public sealed class AssistantKnowledgeService
{
    private static readonly DateTime Updated = new(2026, 8, 24, 0, 0, 0, DateTimeKind.Utc);
    private static readonly HelpArticle[] Articles =
    [
        new("Primeiros passos", "primeiros-passos", "Começar", "Como começo?", "Comece com um hábito pequeno, escolha a frequência e marque a conclusão no Meu Dia. Consistência vale mais que perfeição.", ["começar","início"], true, 1, Updated),
        new("Criar e editar hábitos", "criar-habito", "Hábitos", "Como crio um hábito?", "Abra Hábitos e selecione Criar hábito. Informe nome, frequência e, se quiser, um lembrete. Para editar, abra o hábito e use Editar.", ["criar","editar","hábito"], true, 2, Updated),
        new("Concluir hábitos e streak", "conclusao-streak", "Hábitos", "Como concluo um hábito e como funciona o streak?", "Em Meu Dia, use Concluir. O streak conta sequências de ocorrências planejadas concluídas; um dia não programado não quebra a sequência.", ["concluir","streak","sequência"], true, 3, Updated),
        new("Lembretes e notificações", "lembretes", "Notificações", "Como ativo lembretes?", "Abra Lembretes, escolha um hábito e horário. Para push, permita notificações em Configurações de notificação. Você pode pausar ou ajustar horários.", ["lembrete","push","notificação"], true, 4, Updated),
        new("Desafios", "desafios", "Motivação", "Como funcionam desafios?", "Desafios acompanham um hábito por 7, 30 ou 90 dias. Abra Desafios, escolha um hábito ativo e confirme a duração.", ["desafio","7 dias","30 dias"], true, 5, Updated),
        new("Relatórios e exportações", "relatorios-exportacoes", "Progresso", "Como vejo relatórios ou exporto?", "Abra Relatórios para analisar consistência e evolução. Exportações CSV e a versão para impressão/PDF respeitam os recursos e limites do seu plano.", ["relatório","csv","pdf","exportar"], true, 6, Updated),
        new("Planos Free e Premium", "planos", "Conta", "Qual a diferença entre Free e Premium?", "O Free cobre o acompanhamento essencial com limites. O Premium libera períodos avançados, mais recursos de relatórios, desafios e exportações conforme a página Planos.", ["free","premium","assinatura"], true, 7, Updated),
        new("Instalar o PWA", "pwa", "Aplicativo", "Como instalo o HabitFlow?", "No menu do navegador, escolha Instalar aplicativo ou Adicionar à tela inicial. A instalação depende do suporte do navegador.", ["pwa","instalar","offline"], true, 8, Updated),
        new("Privacidade e suporte", "privacidade-suporte", "Segurança", "Como meus dados são protegidos?", "O HabitFlow separa dados por empresa e usuário. Você pode gerenciar privacidade na conta e abrir um chamado sem enviar senhas, tokens ou cookies.", ["privacidade","lgpd","suporte"], true, 9, Updated)
    ];
    public IReadOnlyList<HelpArticle> List(string? category = null) => Articles.Where(x => x.Active && (string.IsNullOrWhiteSpace(category) || x.Category.Equals(category, StringComparison.OrdinalIgnoreCase))).OrderBy(x => x.Order).ToArray();
    public HelpArticle? Get(string slug) => Articles.FirstOrDefault(x => x.Active && x.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
    public IReadOnlyList<HelpArticle> Search(string? query) { if (string.IsNullOrWhiteSpace(query)) return List(); var terms=query.Split(' ',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries); return Articles.Where(a=>a.Active && terms.Any(t=>(a.Title+' '+a.Question+' '+a.Answer+' '+string.Join(' ',a.Tags)).Contains(t,StringComparison.OrdinalIgnoreCase))).OrderBy(a=>a.Order).ToArray(); }
    public HelpArticle? Match(string message)
    {
        if(message.Contains("crio",StringComparison.OrdinalIgnoreCase)||message.Contains("criar",StringComparison.OrdinalIgnoreCase))return Get("criar-habito");
        if(message.Contains("lembrete",StringComparison.OrdinalIgnoreCase)||message.Contains("notifica",StringComparison.OrdinalIgnoreCase))return Get("lembretes");
        if(message.Contains("streak",StringComparison.OrdinalIgnoreCase)||message.Contains("conclu",StringComparison.OrdinalIgnoreCase))return Get("conclusao-streak");
        if(message.Contains("relat",StringComparison.OrdinalIgnoreCase)||message.Contains("export",StringComparison.OrdinalIgnoreCase)||message.Contains("csv",StringComparison.OrdinalIgnoreCase)||message.Contains("pdf",StringComparison.OrdinalIgnoreCase))return Get("relatorios-exportacoes");
        if(message.Contains("premium",StringComparison.OrdinalIgnoreCase)||message.Contains("plano",StringComparison.OrdinalIgnoreCase)||message.Contains("assinatura",StringComparison.OrdinalIgnoreCase))return Get("planos");
        if(message.Contains("desafio",StringComparison.OrdinalIgnoreCase))return Get("desafios");
        if(message.Contains("pwa",StringComparison.OrdinalIgnoreCase)||message.Contains("instal",StringComparison.OrdinalIgnoreCase))return Get("pwa");
        if(message.Contains("privacidade",StringComparison.OrdinalIgnoreCase)||message.Contains("lgpd",StringComparison.OrdinalIgnoreCase))return Get("privacidade-suporte");
        return null;
    }
}

public sealed class AssistantContextBuilder(IHabitRepository habits, IUserRepository users)
{
    public async Task<AssistantUserContext> BuildAsync(Guid clientId, Guid userId, CancellationToken ct)
    {
        var list = await habits.ListAsync(clientId, userId, ct); var user = await users.GetByIdAsync(userId, ct);
        if (user is null || user.ClientId != clientId) return new(0,0,null,UserPlan.Free,0);
        return new(list.Count(x=>!x.IsArchived&&!x.IsPaused), list.Count(x=>!x.IsArchived&&x.IsPaused), list.FirstOrDefault(x=>!x.IsArchived&&!x.IsPaused)?.Name, user.Plan, list.Count(x=>x.ReminderTime.HasValue&&!x.IsArchived));
    }
}

public sealed class DeterministicAssistantProvider(AssistantKnowledgeService knowledge) : IAssistantProvider
{
    public bool IsConfigured => true;
    public Task<AssistantResponse> GenerateAsync(AssistantRequest request, AssistantUserContext context, CancellationToken ct)
    {
        var article=knowledge.Match(request.Message); var answer=article?.Answer ?? "Não encontrei essa informação no HabitFlow. Posso te direcionar para o suporte.";
        if (request.Message.Contains("meus hábitos",StringComparison.OrdinalIgnoreCase) || request.Message.Contains("meu progresso",StringComparison.OrdinalIgnoreCase))
            answer=$"Você tem {context.ActiveHabits} hábitos ativos e {context.PausedHabits} pausados. "+(context.MostConsistentHabit is null?"Crie um hábito pequeno para começar.":$"Uma boa próxima ação é manter o ritmo de {context.MostConsistentHabit}.")+$" Há {context.Reminders} lembretes configurados e seu plano atual é {context.Plan}.";
        return Task.FromResult(new AssistantResponse(answer,"deterministic","Allowed",article is null?"/support/tickets/new":null,article is null?"Abrir chamado":null));
    }
}

public sealed class AssistantConversationService(IAssistanceRepository repository, AssistantContextBuilder contextBuilder, DeterministicAssistantProvider fallback, AssistantSafetyPolicy safety, ILogger<AssistantConversationService> logger)
{
    public async Task<AssistantResponse> AskAsync(Guid clientId, Guid userId, string message, string correlationId, CancellationToken ct)
    {
        logger.LogInformation(ApplicationEvents.AssistantMessageReceived,"assistant.message.received CorrelationId={CorrelationId} ClientId={ClientId} UserId={UserId} MessageHash={Hash}",correlationId,clientId,userId,Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(message)))[..12]);
        var conversation=await repository.GetOrCreateConversationAsync(clientId,userId,ct); var sanitized=safety.Sanitize(message);
        if (safety.ContainsSensitiveData(message) || safety.IsPromptInjection(message)) { logger.LogWarning(ApplicationEvents.AssistantMessageBlocked,"assistant.message.blocked CorrelationId={CorrelationId} ClientId={ClientId} Reason={Reason}",correlationId,clientId,safety.ContainsSensitiveData(message)?"SensitiveInput":"PromptInjection"); return new("Não posso atender a esse pedido. Não envie segredos ou instruções para contornar as regras do assistente. Posso ajudar com o HabitFlow ou direcionar ao suporte.","safety","Blocked","/support/tickets/new","Abrir suporte"); }
        AssistantResponse response;
        if(safety.IsDestructive(message)) response=new("Essa alteração exige um fluxo seguro próprio e não pode ser feita pelo chat. Posso abrir a tela adequada ou direcionar você ao suporte.","safety","Restricted","/support/tickets/new","Abrir suporte");
        else if(safety.IsOutOfScope(message)) response=new("Posso orientar apenas sobre o uso do HabitFlow. Para decisões médicas, jurídicas ou financeiras, procure um profissional qualificado.","safety","OutOfScope","/help","Ver ajuda");
        else response=await fallback.GenerateAsync(new(message,clientId,userId,correlationId),await contextBuilder.BuildAsync(clientId,userId,ct),ct);
        await repository.AddMessageAsync(new(Guid.NewGuid(),clientId,userId,conversation,"user",sanitized,sanitized,response.SafetyStatus,"local",DateTime.UtcNow,correlationId),ct);
        await repository.AddMessageAsync(new(Guid.NewGuid(),clientId,userId,conversation,"assistant",response.Message,response.Message,response.SafetyStatus,response.Provider,DateTime.UtcNow,correlationId),ct);
        logger.LogInformation(ApplicationEvents.AssistantMessageAnswered,"assistant.message.answered CorrelationId={CorrelationId} ClientId={ClientId} Provider={Provider} Result={SafetyStatus}",correlationId,clientId,response.Provider,response.SafetyStatus); return response;
    }
    public Task DeleteAsync(Guid clientId,Guid userId,CancellationToken ct)=>repository.DeleteHistoryAsync(clientId,userId,ct);
}
