using System.Text;
using HabitFlow.Domain;
using Microsoft.Extensions.Configuration;

namespace HabitFlow.Application;
public sealed class OperationsCenterService(IOperationsCenterRepository repository, AdminAuditService audit, IConfiguration configuration, LogSanitizer sanitizer)
{
    public Task<OperationsSnapshot> GetAsync(CancellationToken ct = default) => repository.GetSnapshotAsync(ct);
    public Task<IReadOnlyList<StructuredLogRow>> LogsAsync(StructuredLogFilter filter, CancellationToken ct = default) => repository.SearchLogsAsync(filter, ct);
    public async Task<StructuredLogRow?> LogAsync(Guid id, CancellationToken ct = default) { var row = await repository.GetLogAsync(id, ct); return row is null ? null : row with { Message = sanitizer.Sanitize(row.Message), Details = sanitizer.Sanitize(row.Details) }; }
    public async Task ResolveAsync(User actor, Guid id, CancellationToken ct = default) { await repository.ResolveAlertAsync(id, actor.Id, ct); await audit.LogAsync(actor, "operational_alert.resolved", "Alerta operacional resolvido", id, null, ct); }
    public async Task<byte[]> ExportAsync(User actor, StructuredLogFilter filter, CancellationToken ct = default)
    {
        var rows = await repository.SearchLogsAsync(filter, ct); var csv = new StringBuilder("data,severidade,evento,modulo,cliente,usuario,correlation_id,mensagem\n");
        foreach (var x in rows) csv.AppendLine(string.Join(',', new[]{x.CreatedAt.ToString("O"),x.Severity,x.EventName,x.Module,x.ClientName??"",x.UserName??"",x.CorrelationId,sanitizer.Sanitize(x.Message)}.Select(Csv)));
        await audit.LogAsync(actor, "admin.log_exported", $"{rows.Count} logs exportados", null, null, ct); return Encoding.UTF8.GetBytes(csv.ToString());
    }
    public async Task<OperationsHealthReport> HealthAsync(CancellationToken ct = default)
    {
        var now=DateTime.UtcNow; var checks=new List<HealthCheckRow>(); void Add(string n,bool ok,string good,string bad,string action)=>checks.Add(new(n,ok?"Saudável":"Falha",ok?good:bad,ok?"Info":"Crítico",now,ok?"Nenhuma ação necessária.":action));
        bool db; try { db=await repository.CanConnectAsync(ct); } catch { db=false; } Add("Banco de dados",db,"Conexão validada.","Não foi possível conectar.","Validar connection string e disponibilidade do PostgreSQL.");
        var pending=db?await repository.PendingMigrationsAsync(ct):-1; Add("Migrations",pending==0,"Todas aplicadas.",pending<0?"Verificação indisponível.":$"{pending} migration(s) pendente(s).","Aplicar migrations incrementais antes do deploy.");
        Add("Script SQL",File.Exists(Path.Combine(AppContext.BaseDirectory,"database","script_completo.sql"))||db,"Schema consultado com sucesso.","Script completo não foi encontrado.","Publicar database/script_completo.sql no artefato.");
        Add("E-mail",Has("Email:SmtpHost"),"SMTP configurado.","SMTP ausente.","Configurar Email:SmtpHost."); Add("Pagamento",Has("MercadoPago:AccessToken"),"Gateway configurado.","Gateway ausente.","Configurar secret do provedor no cofre.");
        Add("IA",Has("OpenAI:ApiKey"),"Provedor configurado.","IA não configurada.","Configurar chave no cofre."); Add("WhatsApp",Has("WhatsApp:AccessToken"),"Canal configurado.","WhatsApp não configurado.","Configurar credenciais do canal.");
        Add("Storage",Directory.Exists(Path.Combine(AppContext.BaseDirectory,"wwwroot")),"Diretório gravável disponível.","Storage indisponível.","Validar volume e permissões."); Add("PWA / manifest",File.Exists(Path.Combine(AppContext.BaseDirectory,"wwwroot","manifest.json")),"Manifest publicado.","Manifest ausente.","Incluir manifest no publish.");
        Add("Jobs agendados",Has("Jobs:Enabled"),"Jobs habilitados.","Jobs não declarados.","Definir Jobs:Enabled e monitorar execuções."); Add("Filas / workers",Has("Workers:Enabled"),"Workers habilitados.","Nenhum worker configurado.","Configurar worker quando filas estiverem em uso.");
        return new(checks.Any(x=>x.Status=="Falha"&&x.Severity=="Crítico")?"Degradado":"Operacional",typeof(OperationsCenterService).Assembly.GetName().Version?.ToString()??"dev",configuration["ASPNETCORE_ENVIRONMENT"]??Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")??"Production",checks);
        bool Has(string key)=>!string.IsNullOrWhiteSpace(configuration[key]);
    }
    private static string Csv(string? value) => "\""+(value??"").Replace("\"","\"\"").Replace("\r"," ").Replace("\n"," ")+"\"";
}
