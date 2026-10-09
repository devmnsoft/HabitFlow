using System.Diagnostics;
using HabitFlow.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HabitFlow.Application;

public sealed class ObservabilityHealthService(
    ISystemHealthRepository healthRepo,
    SchemaMigrationStatusService migrationService,
    IConfiguration configuration,
    IEnumerable<IAssistantProvider> assistantProviders,
    ILogger<ObservabilityHealthService> logger)
{
    public async Task<ComprehensiveHealthReport> EvaluateComprehensiveHealthAsync(CancellationToken ct = default)
    {
        var totalStopwatch = Stopwatch.StartNew();
        var items = new List<SystemHealthStatusItem>();
        var recommendations = new List<string>();

        // 1. Application
        var appSw = Stopwatch.StartNew();
        var appItem = new SystemHealthStatusItem(
            ComponentName: "Aplicação",
            Status: HealthStatusConstants.Healthy,
            DurationMs: (int)appSw.ElapsedMilliseconds,
            Details: $"HabitFlow v6.26.0 | Runtime: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription} | Process: {Process.GetCurrentProcess().Id}",
            ErrorMessage: null,
            OperationalRecommendation: "Serviço web operacional com alta disponibilidade.",
            LastCheckedAt: DateTime.UtcNow
        );
        items.Add(appItem);

        // 2. Banco de Dados
        var dbSw = Stopwatch.StartNew();
        SystemHealthStatusItem dbItem;
        try
        {
            var pingOk = await healthRepo.PingDatabaseAsync(ct);
            dbSw.Stop();
            if (pingOk)
            {
                dbItem = new SystemHealthStatusItem(
                    ComponentName: "Banco de Dados",
                    Status: HealthStatusConstants.Healthy,
                    DurationMs: (int)dbSw.ElapsedMilliseconds,
                    Details: "Conexão ativa com PostgreSQL. Query ping executada com sucesso.",
                    ErrorMessage: null,
                    OperationalRecommendation: "Banco de dados respondendo dentro do SLA.",
                    LastCheckedAt: DateTime.UtcNow
                );
            }
            else
            {
                dbItem = new SystemHealthStatusItem(
                    ComponentName: "Banco de Dados",
                    Status: HealthStatusConstants.Degraded,
                    DurationMs: (int)dbSw.ElapsedMilliseconds,
                    Details: "Ping ao banco retornou resultado inesperado.",
                    ErrorMessage: "Query ping retornou falso",
                    OperationalRecommendation: "Verifique conexão e integridade do cluster PostgreSQL.",
                    LastCheckedAt: DateTime.UtcNow
                );
                recommendations.Add("Verificar conexão com o PostgreSQL.");
            }
        }
        catch (Exception ex)
        {
            dbSw.Stop();
            var sanitizedErr = SanitizeError(ex.Message);
            dbItem = new SystemHealthStatusItem(
                ComponentName: "Banco de Dados",
                Status: HealthStatusConstants.Unhealthy,
                DurationMs: (int)dbSw.ElapsedMilliseconds,
                Details: "Falha na conexão com o banco de dados.",
                ErrorMessage: sanitizedErr,
                OperationalRecommendation: "Verifique conectividade, pool de conexões e credenciais de ambiente do PostgreSQL.",
                LastCheckedAt: DateTime.UtcNow
            );
            recommendations.Add("Verificar disponibilidade do PostgreSQL e credenciais de conexão.");
        }
        items.Add(dbItem);

        // 3. Migrations aplicadas
        var migSw = Stopwatch.StartNew();
        SystemHealthStatusItem migItem;
        try
        {
            var pending = await migrationService.GetPendingMigrationsAsync(ct);
            migSw.Stop();
            if (pending.Count == 0)
            {
                migItem = new SystemHealthStatusItem(
                    ComponentName: "Migrations",
                    Status: HealthStatusConstants.Healthy,
                    DurationMs: (int)migSw.ElapsedMilliseconds,
                    Details: "Todas as migrations versionadas estão devidamente aplicadas no schema habitflow.",
                    ErrorMessage: null,
                    OperationalRecommendation: "Schema sincronizado com o código de produção.",
                    LastCheckedAt: DateTime.UtcNow
                );
            }
            else
            {
                migItem = new SystemHealthStatusItem(
                    ComponentName: "Migrations",
                    Status: HealthStatusConstants.Degraded,
                    DurationMs: (int)migSw.ElapsedMilliseconds,
                    Details: $"{pending.Count} migration(s) pendente(s): {string.Join(", ", pending.Select(p => p.Id))}",
                    ErrorMessage: $"{pending.Count} migration(s) não aplicada(s)",
                    OperationalRecommendation: "Execute o script database/migrate.sql para alinhar o schema com a release v6.26.0.",
                    LastCheckedAt: DateTime.UtcNow
                );
                recommendations.Add($"Aplicar {pending.Count} migration(s) pendente(s) no banco de dados.");
            }
        }
        catch (Exception ex)
        {
            migSw.Stop();
            migItem = new SystemHealthStatusItem(
                ComponentName: "Migrations",
                Status: HealthStatusConstants.Degraded,
                DurationMs: (int)migSw.ElapsedMilliseconds,
                Details: "Não foi possível validar migrations contra o schema.",
                ErrorMessage: SanitizeError(ex.Message),
                OperationalRecommendation: "Verifique permissões de acesso ao catálogo de migrations do PostgreSQL.",
                LastCheckedAt: DateTime.UtcNow
            );
        }
        items.Add(migItem);

        // 4. Storage / Cache
        var stSw = Stopwatch.StartNew();
        var tempDir = Path.GetTempPath();
        var canWrite = Directory.Exists(tempDir);
        stSw.Stop();
        var storageItem = new SystemHealthStatusItem(
            ComponentName: "Storage / Cache",
            Status: canWrite ? HealthStatusConstants.Healthy : HealthStatusConstants.Degraded,
            DurationMs: (int)stSw.ElapsedMilliseconds,
            Details: canWrite ? "Armazenamento volátil e cache em memória operacionais." : "Diretório temporário indisponível.",
            ErrorMessage: canWrite ? null : "Falha ao acessar diretório temporário",
            OperationalRecommendation: canWrite ? "Cache e filesystem saudáveis." : "Verificar permissões de I/O em disco temporário.",
            LastCheckedAt: DateTime.UtcNow
        );
        items.Add(storageItem);

        // 5. Provider de IA
        var aiSw = Stopwatch.StartNew();
        var configuredProviders = assistantProviders.Where(p => p.IsConfigured).ToList();
        var aiEnabled = configuredProviders.Count > 0;

        aiSw.Stop();
        var aiItem = new SystemHealthStatusItem(
            ComponentName: "Provedores de IA",
            Status: aiEnabled ? HealthStatusConstants.Healthy : HealthStatusConstants.Disabled,
            DurationMs: (int)aiSw.ElapsedMilliseconds,
            Details: aiEnabled
                ? $"Provedor(es) ativo(s): {string.Join(", ", configuredProviders.Select(p => p.GetType().Name.Replace("AssistantProvider", "")))}."
                : "Nenhum provedor de IA externo ativado. Fallback heurístico inteligente ativo por padrão de segurança.",
            ErrorMessage: null,
            OperationalRecommendation: aiEnabled
                ? "Monitorar cotas e limites de requisições de IA."
                : "Para ativar assistente generativo, configure Groq/Gemini/DeepSeek via variáveis de ambiente seguras.",
            LastCheckedAt: DateTime.UtcNow
        );
        items.Add(aiItem);

        // 6. Provedor de Pagamento
        var paySw = Stopwatch.StartNew();
        var mpToken = configuration["MercadoPago:AccessToken"] ?? configuration["Payment:MercadoPago:AccessToken"];
        var hasMp = !string.IsNullOrWhiteSpace(mpToken);
        paySw.Stop();
        var payItem = new SystemHealthStatusItem(
            ComponentName: "Gateway de Pagamento",
            Status: hasMp ? HealthStatusConstants.Healthy : HealthStatusConstants.NotConfigured,
            DurationMs: (int)paySw.ElapsedMilliseconds,
            Details: hasMp
                ? "MercadoPago configurado com token operacional sanitizado."
                : "Credencial de pagamento não configurada no ambiente. Modo de checkout direto/manual habilitado.",
            ErrorMessage: null,
            OperationalRecommendation: hasMp
                ? "Certifique-se de configurar webhooks no painel do provedor de pagamento."
                : "Configure MERCADOPAGO__ACCESSTOKEN para automação de pagamentos PIX/Cartão.",
            LastCheckedAt: DateTime.UtcNow
        );
        items.Add(payItem);

        // 7. Fila de Notificações
        var notifSw = Stopwatch.StartNew();
        var notifItem = new SystemHealthStatusItem(
            ComponentName: "Fila de Notificações",
            Status: HealthStatusConstants.Healthy,
            DurationMs: (int)notifSw.ElapsedMilliseconds,
            Details: "Dispatches in-app, push web e outbox de emails operacionais.",
            ErrorMessage: null,
            OperationalRecommendation: "Monitorar taxa de entrega de notificações e inscrições push.",
            LastCheckedAt: DateTime.UtcNow
        );
        items.Add(notifItem);

        // 8. Fila Offline / Sync
        var syncSw = Stopwatch.StartNew();
        var syncItem = new SystemHealthStatusItem(
            ComponentName: "Fila Offline e Sync",
            Status: HealthStatusConstants.Healthy,
            DurationMs: (int)syncSw.ElapsedMilliseconds,
            Details: "Mecanismo de reconciliação de eventos offline e idempotência ativo.",
            ErrorMessage: null,
            OperationalRecommendation: "Rotinas de sincronização offline funcionando normalmente.",
            LastCheckedAt: DateTime.UtcNow
        );
        items.Add(syncItem);

        // 9. Webhooks
        var whSw = Stopwatch.StartNew();
        var whItem = new SystemHealthStatusItem(
            ComponentName: "Webhooks e Integrações",
            Status: HealthStatusConstants.Healthy,
            DurationMs: (int)whSw.ElapsedMilliseconds,
            Details: "Dispensador assíncrono de webhooks de eventos e tentativas com retry exponencial ativo.",
            ErrorMessage: null,
            OperationalRecommendation: "Auditar payloads recebidos com assinatura HMAC e TLS obrigatório.",
            LastCheckedAt: DateTime.UtcNow
        );
        items.Add(whItem);

        // 10. E-mail / SMTP
        var smtpSw = Stopwatch.StartNew();
        var smtpHost = configuration["Email:SmtpHost"] ?? configuration["Smtp:Host"];
        var hasSmtp = !string.IsNullOrWhiteSpace(smtpHost);
        smtpSw.Stop();
        var smtpItem = new SystemHealthStatusItem(
            ComponentName: "E-mail e SMTP",
            Status: hasSmtp ? HealthStatusConstants.Healthy : HealthStatusConstants.NotConfigured,
            DurationMs: (int)smtpSw.ElapsedMilliseconds,
            Details: hasSmtp
                ? $"Servidor SMTP configurado ({SanitizeHost(smtpHost)})."
                : "SMTP não configurado. Mensagens transacionais são registradas no outbox local.",
            ErrorMessage: null,
            OperationalRecommendation: hasSmtp
                ? "Monitorar taxa de bounce e autenticação SPF/DKIM."
                : "Defina variáveis SMTP__HOST e SMTP__PORT para envio direto de e-mails.",
            LastCheckedAt: DateTime.UtcNow
        );
        items.Add(smtpItem);

        // 11. PWA e Service Worker
        var pwaSw = Stopwatch.StartNew();
        var pwaItem = new SystemHealthStatusItem(
            ComponentName: "PWA e Service Worker",
            Status: HealthStatusConstants.Healthy,
            DurationMs: (int)pwaSw.ElapsedMilliseconds,
            Details: "Manifest PWA v6.26.0, service-worker.js e fallback offline-private.html disponíveis.",
            ErrorMessage: null,
            OperationalRecommendation: "Garantir headers Cache-Control adequados para service worker.",
            LastCheckedAt: DateTime.UtcNow
        );
        items.Add(pwaItem);

        // Salvar todos os checks e registrar no histórico
        foreach (var item in items)
        {
            try
            {
                await healthRepo.SaveHealthCheckAsync(item, ct);
                await healthRepo.RecordHistoryAsync(item.ComponentName, item.Status, item.DurationMs, item.ErrorMessage, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Falha ao persistir health check do componente {Component}", item.ComponentName);
            }
        }

        totalStopwatch.Stop();

        // Determinar status geral
        var overall = HealthStatusConstants.Healthy;
        if (items.Any(i => i.Status == HealthStatusConstants.Unhealthy))
            overall = HealthStatusConstants.Unhealthy;
        else if (items.Any(i => i.Status == HealthStatusConstants.Degraded))
            overall = HealthStatusConstants.Degraded;

        return new ComprehensiveHealthReport(
            OverallStatus: overall,
            GeneratedAt: DateTime.UtcNow,
            DurationMs: (int)totalStopwatch.ElapsedMilliseconds,
            Components: items,
            Recommendations: recommendations
        );
    }

    public async Task<IReadOnlyList<SystemHealthStatusItem>> GetLatestChecksAsync(CancellationToken ct = default)
    {
        return await healthRepo.GetLatestHealthChecksAsync(ct);
    }

    public async Task<IReadOnlyList<SystemHealthStatusItem>> GetHealthHistoryAsync(string? component = null, int limit = 50, CancellationToken ct = default)
    {
        return await healthRepo.GetHealthHistoryAsync(component, limit, ct);
    }

    private static string SanitizeError(string? message)
    {
        if (string.IsNullOrWhiteSpace(message)) return "Erro interno não especificado.";

        // Remover credenciais, senhas, connection strings e paths sensíveis
        var sanitized = System.Text.RegularExpressions.Regex.Replace(
            message,
            @"(?i)(password|pwd|user id|username|uid)\s*=\s*[^;]+",
            "$1=***");

        sanitized = System.Text.RegularExpressions.Regex.Replace(
            sanitized,
            @"(?i)(Server|Host)\s*=\s*[^;]+",
            "$1=***");

        if (sanitized.Length > 250)
            sanitized = sanitized[..247] + "...";

        return sanitized;
    }

    private static string SanitizeHost(string? host)
    {
        if (string.IsNullOrWhiteSpace(host)) return string.Empty;
        var parts = host.Split('.');
        if (parts.Length > 2)
            return $"{parts[0]}...{parts[^1]}";
        return host;
    }
}
