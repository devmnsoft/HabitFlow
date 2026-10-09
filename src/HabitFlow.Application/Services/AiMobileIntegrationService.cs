using System.Text.RegularExpressions;
using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HabitFlow.Application;

public sealed class AiMobileIntegrationService(
    IOptions<AiOptions> aiOptions,
    IHabitCompletionRepository completionRepo,
    IHabitRepository habitRepo,
    PlanEntitlementService entitlements,
    AuditService audit,
    ILogger<AiMobileIntegrationService> logger)
{
    private static readonly Regex SensitiveEmailRegex = new(@"[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+", RegexOptions.Compiled);
    private static readonly Regex SensitiveTokenRegex = new(@"(?i)(bearer\s+[a-z0-9._-]+|hf_live_[a-z0-9]+|whsec_[a-z0-9]+)", RegexOptions.Compiled);

    public static string SanitizeInput(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var sanitized = SensitiveEmailRegex.Replace(text, "[EMAIL_PROTEGIDO]");
        sanitized = SensitiveTokenRegex.Replace(sanitized, "[TOKEN_PROTEGIDO]");
        return sanitized;
    }

    public async Task<string> SuggestBestReminderTimeAsync(
        Guid clientId,
        Guid userId,
        Guid habitId,
        CancellationToken ct = default)
    {
        var habit = await habitRepo.GetAsync(clientId, userId, habitId, ct);
        if (habit is null || habit.IsArchived)
        {
            return "08:00";
        }

        var history = await completionRepo.ListByUserAsync(userId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)), ct);
        var habitCompletions = history.Where(c => c.HabitId == habitId).ToList();
        if (habitCompletions.Count == 0)
        {
            // Fallback padrão se não há conclusões registradas
            return "08:00";
        }

        // Analisa o horário médio em que o usuário completa o hábito
        var hours = habitCompletions.Select(h => h.CreatedAt.TimeOfDay.Hours).ToList();
        var avgHour = (int)Math.Round(hours.Average());
        avgHour = Math.Clamp(avgHour, 6, 22);

        // Se o provedor de IA estiver ativado e autorizado
        if (aiOptions.Value.Enabled)
        {
            logger.LogInformation("Gerando sugestão de horário com IA para hábito {HabitId}", habitId);
        }

        return $"{avgHour:D2}:00";
    }

    public Task<string> SummarizeWeeklyReviewAsync(
        WeeklyReview review,
        CancellationToken ct = default)
    {
        var isCompleted = review.CompletedAt.HasValue || review.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase);

        // Fallback determinístico seguro
        var summary = isCompleted
            ? $"Revisão semanal concluída com sucesso para o período {review.PeriodStart:dd/MM} a {review.PeriodEnd:dd/MM}. Mantenha esse ritmo consistente e anote seus aprendizados."
            : $"Revisão semanal pendente para o período {review.PeriodStart:dd/MM} a {review.PeriodEnd:dd/MM}. Reserve 5 minutos para refletir sobre seus hábitos e planejar a próxima semana.";

        return Task.FromResult(SanitizeInput(summary));
    }

    public Task<string> GenerateRecoveryPlanAsync(
        string habitName,
        int currentStreak,
        CancellationToken ct = default)
    {
        var safeHabit = SanitizeInput(habitName);
        var plan = $"Plano de retomada para '{safeHabit}':\n" +
                   $"1. Reduza a meta para 5 minutos por dia nos próximos 3 dias.\n" +
                   $"2. Ancore este hábito logo após uma rotina já consolidada (ex: café da manhã).\n" +
                   $"3. Não se cobre perfeição: consistência mínima supera intensidade pontual.";

        return Task.FromResult(plan);
    }

    public Task<string> ExplainApiEndpointAsync(
        string endpoint,
        string method,
        CancellationToken ct = default)
    {
        endpoint = SanitizeInput(endpoint);
        method = method.ToUpperInvariant();

        var curl = $"curl -X {method} \"https://app.habitflow.com.br{endpoint}\" \\\n" +
                   $"  -H \"X-Api-Key: hf_live_SEU_TOKEN_AQUI\" \\\n" +
                   $"  -H \"Content-Type: application/json\"";

        var python = $"import requests\n\n" +
                     $"headers = {{'X-Api-Key': 'hf_live_SEU_TOKEN_AQUI'}}\n" +
                     $"response = requests.{method.ToLowerInvariant()}('https://app.habitflow.com.br{endpoint}', headers=headers)\n" +
                     $"print(response.json())";

        var explanation = $"### Exemplo de Integração ({method} {endpoint})\n\n" +
                          $"**cURL:**\n```bash\n{curl}\n```\n\n" +
                          $"**Python:**\n```python\n{python}\n```";

        return Task.FromResult(explanation);
    }

    public Task<string> GenerateWebhookDocsAsync(
        string eventName,
        CancellationToken ct = default)
    {
        eventName = SanitizeInput(eventName);

        var doc = $"### Documentação do Evento: `{eventName}`\n\n" +
                  $"Este evento é disparado via POST para os webhooks com assinatura HMAC-SHA256 no cabeçalho `X-HabitFlow-Signature`.\n\n" +
                  $"**Exemplo de Payload:**\n```json\n" +
                  $"{{\n" +
                  $"  \"event\": \"{eventName}\",\n" +
                  $"  \"eventId\": \"a1b2c3d4-0000-0000-0000-000000000000\",\n" +
                  $"  \"clientId\": \"c1d2e3f4-0000-0000-0000-000000000000\",\n" +
                  $"  \"timestamp\": \"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}\",\n" +
                  $"  \"data\": {{\n" +
                  $"    \"status\": \"success\",\n" +
                  $"    \"entityId\": \"e1f2a3b4-0000-0000-0000-000000000000\"\n" +
                  $"  }}\n" +
                  $"}}\n```";

        return Task.FromResult(doc);
    }
}
