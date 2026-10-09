using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed record OperationalAiSuggestion(
    string Suggestion,
    string Provider,
    string Model,
    bool RequiresHumanReview = true
);

public sealed class OperationalAiService(
    IEnumerable<IAssistantProvider> providers,
    AssistantSafetyPolicy safety,
    ILogger<OperationalAiService> logger)
{
    private IAssistantProvider? ActiveProvider =>
        providers.FirstOrDefault(p => p.IsConfigured) ?? providers.FirstOrDefault();

    public async Task<OperationalAiSuggestion> SuggestTicketReplyAsync(
        string subject, string description, string? context, CancellationToken ct = default)
    {
        var sanitizedSubject = safety.Sanitize(MaskSensitiveData(subject));
        var sanitizedDesc = safety.Sanitize(MaskSensitiveData(description));

        var prompt = $"Como especialista em suporte do HabitFlow SaaS, sugira uma resposta formal, clara e empática para o chamado a seguir.\n" +
                     $"Assunto: {sanitizedSubject}\nDescrição: {sanitizedDesc}\n" +
                     $"Diretrizes: Seja cordial, explique com passos simples, não invente dados técnicos e não prometa ressarcimentos sem conferência.";

        var provider = ActiveProvider;
        if (provider != null && provider.IsConfigured)
        {
            try
            {
                var req = new AssistantRequest(prompt, Guid.Empty, Guid.Empty, Guid.NewGuid().ToString("N"));
                var userCtx = new AssistantUserContext(1, 0, UserPlan.Premium, 1);
                var response = await provider.GenerateAsync(req, userCtx, ct);
                if (response.SafetyStatus == "Allowed" && !string.IsNullOrWhiteSpace(response.Message))
                {
                    return new OperationalAiSuggestion(response.Message, response.Provider, "Operational-LLM");
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Falha ao gerar sugestão de suporte via LLM externa. Usando fallback seguro.");
            }
        }

        // Fallback heurístico inteligente e seguro
        var fallback = $"Olá! Agradecemos o contato.\n\n" +
                       $"Recebemos sua solicitação sobre \"{sanitizedSubject}\" e nossa equipe já está investigando o caso para garantir que seus hábitos e rotinas continuem sincronizados.\n\n" +
                       $"Para agilizarmos o diagnóstico, por gentileza confirme se a situação ocorre tanto no navegador quanto no aplicativo móvel.\n\n" +
                       $"Seguimos à disposição!";

        return new OperationalAiSuggestion(fallback, "LocalGuard", "RuleEngine-v6.21.0");
    }

    public async Task<OperationalAiSuggestion> SummarizeCustomerHistoryAsync(
        string clientName, string plan, int activeUsers, int habits, int completions7d, string subscriptionStatus, CancellationToken ct = default)
    {
        var sanitizedName = MaskSensitiveData(clientName);
        var prompt = $"Resuma a tração e adoção da conta '{sanitizedName}' (Plano {plan}, Status {subscriptionStatus}): " +
                     $"{activeUsers} usuários ativos, {habits} hábitos cadastrados e {completions7d} conclusões nos últimos 7 dias. " +
                     $"Destaque o nível de retenção em até 3 frases objetivas.";

        var provider = ActiveProvider;
        if (provider != null && provider.IsConfigured)
        {
            try
            {
                var req = new AssistantRequest(prompt, Guid.Empty, Guid.Empty, Guid.NewGuid().ToString("N"));
                var userCtx = new AssistantUserContext(habits, 0, UserPlan.Premium, 0);
                var response = await provider.GenerateAsync(req, userCtx, ct);
                if (response.SafetyStatus == "Allowed" && !string.IsNullOrWhiteSpace(response.Message))
                    return new OperationalAiSuggestion(response.Message, response.Provider, "Operational-LLM");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Falha ao resumir cliente via LLM externa. Usando fallback.");
            }
        }

        var engagement = completions7d > 10 ? "alto engajamento e ritmo regular de conclusão" :
                         completions7d > 0 ? "engajamento moderado com espaço para consolidação de rotina" :
                         "baixo engajamento recente, requerendo contato proativo de CS";

        var summary = $"O cliente {sanitizedName} ({plan}) opera com {activeUsers} membros ativos e {habits} hábitos cadastrados. " +
                      $"Apresenta {engagement} ({completions7d} conclusões na semana). " +
                      $"Status financeiro/contratual atual: {subscriptionStatus}.";

        return new OperationalAiSuggestion(summary, "LocalGuard", "RuleEngine-v6.21.0");
    }

    public async Task<OperationalAiSuggestion> ExplainIncidentAsync(
        string title, string impact, string severity, CancellationToken ct = default)
    {
        var sanitizedTitle = safety.Sanitize(MaskSensitiveData(title));
        var sanitizedImpact = safety.Sanitize(MaskSensitiveData(impact));

        var prompt = $"Escreva uma comunicação transparente e tranquilizadora em português para clientes do SaaS sobre o seguinte incidente:\n" +
                     $"Título: {sanitizedTitle}\nSeveridade: {severity}\nImpacto: {sanitizedImpact}\n" +
                     $"Oriente que a equipe de engenharia já está atuando na mitigação.";

        var provider = ActiveProvider;
        if (provider != null && provider.IsConfigured)
        {
            try
            {
                var req = new AssistantRequest(prompt, Guid.Empty, Guid.Empty, Guid.NewGuid().ToString("N"));
                var userCtx = new AssistantUserContext(0, 0, UserPlan.Premium, 0);
                var response = await provider.GenerateAsync(req, userCtx, ct);
                if (response.SafetyStatus == "Allowed" && !string.IsNullOrWhiteSpace(response.Message))
                    return new OperationalAiSuggestion(response.Message, response.Provider, "Operational-LLM");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Falha ao redigir incidente via LLM. Usando fallback.");
            }
        }

        var message = $"COMUNICADO OPERACIONAL ({severity}): Identificamos uma instabilidade que pode afetar {sanitizedImpact}. " +
                      $"Nossa equipe técnica já está em ação e aplicando medidas de contenção para restaurar a normalidade com segurança. " +
                      $"Manteremos você atualizado conforme o status evoluir.";

        return new OperationalAiSuggestion(message, "LocalGuard", "RuleEngine-v6.21.0");
    }

    public async Task<OperationalAiSuggestion> ExplainChurnRiskAsync(
        string clientName, int score, IReadOnlyList<string> riskFactors, CancellationToken ct = default)
    {
        var factors = string.Join("; ", riskFactors);
        var prompt = $"Como estrategista de CS, explique por que a conta '{clientName}' está com score de saúde {score}/100 e fatores de risco: {factors}. " +
                     $"Sugira 2 ações práticas para o time de suporte/CS.";

        var provider = ActiveProvider;
        if (provider != null && provider.IsConfigured)
        {
            try
            {
                var req = new AssistantRequest(prompt, Guid.Empty, Guid.Empty, Guid.NewGuid().ToString("N"));
                var userCtx = new AssistantUserContext(0, 0, UserPlan.Premium, 0);
                var response = await provider.GenerateAsync(req, userCtx, ct);
                if (response.SafetyStatus == "Allowed" && !string.IsNullOrWhiteSpace(response.Message))
                    return new OperationalAiSuggestion(response.Message, response.Provider, "Operational-LLM");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Falha ao gerar churn risk explanation via LLM.");
            }
        }

        var explanation = $"A conta '{clientName}' apresenta score {score} devido a: {factors}. " +
                          $"Recomenda-se: 1) Agendar alinhamento de ativação e acompanhamento de rotina com o gestor do tenant; " +
                          $"2) Verificar pendências contratuais ou financeiras para evitar interrupções no acesso aos hábitos.";

        return new OperationalAiSuggestion(explanation, "LocalGuard", "RuleEngine-v6.21.0");
    }

    public async Task<OperationalAiSuggestion> ExplainIncidentRootCauseAsync(
        string title, string module, string severity, string description, CancellationToken ct = default)
    {
        var sanitizedTitle = safety.Sanitize(MaskSensitiveData(title));
        var sanitizedModule = safety.Sanitize(MaskSensitiveData(module));
        var sanitizedDesc = safety.Sanitize(MaskSensitiveData(description));

        var prompt = $"Analise este incidente operacional no SaaS HabitFlow e sugira a causa raiz provável e plano de ação:\n" +
                     $"Título: {sanitizedTitle}\nMódulo: {sanitizedModule}\nSeveridade: {severity}\nDetalhes: {sanitizedDesc}\n" +
                     $"Forneça: 1) Causa provável; 2) Ações imediatas de mitigação; 3) Medidas preventivas futuras.";

        var provider = ActiveProvider;
        if (provider != null && provider.IsConfigured)
        {
            try
            {
                var req = new AssistantRequest(prompt, Guid.Empty, Guid.Empty, Guid.NewGuid().ToString("N"));
                var userCtx = new AssistantUserContext(0, 0, UserPlan.Premium, 0);
                var response = await provider.GenerateAsync(req, userCtx, ct);
                if (response.SafetyStatus == "Allowed" && !string.IsNullOrWhiteSpace(response.Message))
                    return new OperationalAiSuggestion(response.Message, response.Provider, "Operational-LLM");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Falha ao gerar análise de causa raiz via LLM.");
            }
        }

        var heuristic = $"Diagnóstico preliminar para {sanitizedTitle} ({severity}) no módulo {sanitizedModule}:\n" +
                        $"• Causa provável: Instabilidade transitória em dependências externas ou latência na camada de persistência/rede.\n" +
                        $"• Ação recomendada: Avaliar telemetria e health checks dos 11 componentes; verificar se há contenção de recursos ou bloqueio em transações.\n" +
                        $"• Prevenção: Estabelecer circuit breaker e alarmes proativos para o módulo {sanitizedModule}.";

        return new OperationalAiSuggestion(heuristic, "LocalGuard", "RuleEngine-v6.26.0");
    }

    public async Task<OperationalAiSuggestion> SummarizeHealthChecksAsync(
        IEnumerable<SystemHealthStatusItem> checks, CancellationToken ct = default)
    {
        var checkList = checks.ToList();
        var unhealthyCount = checkList.Count(c => c.Status == HealthStatusConstants.Unhealthy);
        var degradedCount = checkList.Count(c => c.Status == HealthStatusConstants.Degraded);
        var disabledCount = checkList.Count(c => c.Status == HealthStatusConstants.Disabled || c.Status == HealthStatusConstants.NotConfigured);

        var prompt = $"Resuma a saúde dos seguintes componentes do SaaS HabitFlow: {checkList.Count} componentes avaliados. " +
                     $"Unhealthy: {unhealthyCount}, Degraded: {degradedCount}, Disabled/NotConfigured: {disabledCount}. " +
                     $"Destaque o impacto operacional geral em 3 frases curtas.";

        var provider = ActiveProvider;
        if (provider != null && provider.IsConfigured)
        {
            try
            {
                var req = new AssistantRequest(prompt, Guid.Empty, Guid.Empty, Guid.NewGuid().ToString("N"));
                var userCtx = new AssistantUserContext(0, 0, UserPlan.Premium, 0);
                var response = await provider.GenerateAsync(req, userCtx, ct);
                if (response.SafetyStatus == "Allowed" && !string.IsNullOrWhiteSpace(response.Message))
                    return new OperationalAiSuggestion(response.Message, response.Provider, "Operational-LLM");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Falha ao resumir health checks via LLM.");
            }
        }

        var statusDesc = unhealthyCount > 0 ? "ATENÇÃO CRÍTICA: Há componentes em estado Unhealthy que exigem intervenção imediata da engenharia."
                       : degradedCount > 0 ? "ATENÇÃO OPERACIONAL: O sistema opera com degradação parcial em dependências secundárias."
                       : "SISTEMA ESTÁVEL: Todos os serviços essenciais (banco, aplicação, cache) estão respondendo dentro dos parâmetros ideais de SLA.";

        var summary = $"{statusDesc} Total de {checkList.Count} subsistemas monitorados. Componentes informativos/desabilitados ({disabledCount}) operam com contingência segura.";

        return new OperationalAiSuggestion(summary, "LocalGuard", "RuleEngine-v6.26.0");
    }

    public async Task<OperationalAiSuggestion> GenerateReleaseSummaryAsync(
        string version, IEnumerable<ReleaseChecklistItem> items, CancellationToken ct = default)
    {
        var itemList = items.ToList();
        var done = itemList.Count(i => i.IsCompleted);
        var total = itemList.Count;

        var prompt = $"Gere um resumo executivo da release {version} do HabitFlow: {done} de {total} itens concluídos no checklist de homologação. " +
                     $"Destaque prontidão para produção e observância de LGPD, backup e governança.";

        var provider = ActiveProvider;
        if (provider != null && provider.IsConfigured)
        {
            try
            {
                var req = new AssistantRequest(prompt, Guid.Empty, Guid.Empty, Guid.NewGuid().ToString("N"));
                var userCtx = new AssistantUserContext(0, 0, UserPlan.Premium, 0);
                var response = await provider.GenerateAsync(req, userCtx, ct);
                if (response.SafetyStatus == "Allowed" && !string.IsNullOrWhiteSpace(response.Message))
                    return new OperationalAiSuggestion(response.Message, response.Provider, "Operational-LLM");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Falha ao gerar release summary via LLM.");
            }
        }

        var readyText = done == total
            ? "Todas as etapas de validação e governança foram concluídas com sucesso. Release homologada para produção."
            : $"Existem {total - done} itens pendentes de confirmação no checklist antes do go-live definitivo.";

        var fallback = $"Relatório Executivo de Release ({version}): O checklist operacional registra {done}/{total} itens concluídos. {readyText} Governança de dados LGPD e procedimentos de backup/restore encontram-se documentados e ativos.";

        return new OperationalAiSuggestion(fallback, "LocalGuard", "RuleEngine-v6.26.0");
    }

    public static string MaskSensitiveData(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var masked = System.Text.RegularExpressions.Regex.Replace(
            text,
            @"\b(\d{3})\.?(\d{3})\.?(\d{3})-?(\d{2})\b",
            "$1.***.***-$4");

        masked = System.Text.RegularExpressions.Regex.Replace(
            masked,
            @"\b(\d{2})\.?(\d{3})\.?(\d{3})\/?(\d{4})-?(\d{2})\b",
            "$1.***.***/$4-$5");

        masked = System.Text.RegularExpressions.Regex.Replace(
            masked,
            @"\b(?:\d[ -]*?){13,16}\b",
            "****-****-****-****");

        masked = System.Text.RegularExpressions.Regex.Replace(
            masked,
            @"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b",
            "[email-removido]",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        masked = System.Text.RegularExpressions.Regex.Replace(
            masked,
            @"(?i)\b(token|api[_-]?key|secret|senha|password)\s*[:=]\s*\S+",
            "$1=[REMOVIDO]");

        return masked;
    }
}
