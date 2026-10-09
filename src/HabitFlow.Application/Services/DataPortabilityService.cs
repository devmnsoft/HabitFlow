using System.Text;
using System.Text.Json;
using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed class DataPortabilityService(
    IDataPortabilityRepository portabilityRepo,
    IHabitRepository habitRepo,
    IHabitCompletionRepository completionRepo,
    IUserGoalRepository goalRepo,
    IWeeklyReviewRepository reviewRepo,
    INotificationRepository notificationRepo,
    PlanEntitlementService entitlements,
    AuditService audit,
    ILogger<DataPortabilityService> logger)
{
    public async Task<(byte[] Bytes, string ContentType, string FileName)> ExportUserDataAsync(
        Guid clientId,
        Guid userId,
        string userEmail,
        string userName,
        string format,
        CancellationToken ct = default)
    {
        format = format.ToLowerInvariant() == "csv" ? "csv" : "json";
        var habits = await habitRepo.ListAsync(clientId, userId, ct);
        var goals = await goalRepo.ListAsync(clientId, userId, ct);
        var currentReview = await reviewRepo.GetAsync(clientId, userId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)), ct);
        var reviews = currentReview is not null ? new List<WeeklyReview> { currentReview } : new List<WeeklyReview>();
        var notifPage = await notificationRepo.SearchAsync(new NotificationQuery(clientId, userId, Filter: "all", Page: 1, PageSize: 50), ct);
        var notifications = notifPage.Items;
        var completions = await completionRepo.ListByUserAsync(userId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-180)), ct);

        var totalRecords = habits.Count + goals.Count + reviews.Count + notifications.Count + completions.Count;
        byte[] bytes;
        string contentType;
        string fileName;

        if (format == "csv")
        {
            var sb = new StringBuilder();
            sb.AppendLine("# HABITFLOW - EXPORTACAO DE DADOS PESSOAIS (PORTABILIDADE LGPD)");
            sb.AppendLine($"# Usuario: {userEmail} | Data: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine();
            sb.AppendLine("=== HABITOS ===");
            sb.AppendLine("Id,Nome,Categoria,Frequencia,DiasPorSemana,Pausado,CriadoEm");
            foreach (var h in habits)
            {
                sb.AppendLine($"\"{h.Id}\",\"{EscapeCsv(h.Name)}\",\"{EscapeCsv(h.Category)}\",\"{h.FrequencyType}\",{h.TargetPerWeek ?? 7},{h.IsPaused},\"{h.CreatedAt:yyyy-MM-dd HH:mm:ss}\"");
            }
            sb.AppendLine();
            sb.AppendLine("=== CONCLUSOES ===");
            sb.AppendLine("Id,HabitoId,DataConclusao,CompletadoEm");
            foreach (var c in completions)
            {
                sb.AppendLine($"\"{c.Id}\",\"{c.HabitId}\",\"{c.CompletedDate:yyyy-MM-dd}\",\"{c.CreatedAt:yyyy-MM-dd HH:mm:ss}\"");
            }
            sb.AppendLine();
            sb.AppendLine("=== METAS ===");
            sb.AppendLine("Id,Titulo,TipoAlvo,ValorAlvo,ValorAtual,Status,DataFim");
            foreach (var g in goals)
            {
                sb.AppendLine($"\"{g.Id}\",\"{EscapeCsv(g.Title)}\",\"{g.TargetType}\",{g.TargetValue},{g.CurrentValue},\"{g.Status}\",\"{g.EndDate:yyyy-MM-dd}\"");
            }

            bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            contentType = "text/csv; charset=utf-8";
            fileName = $"habitflow-export-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
        }
        else
        {
            var package = new UserDataExportPackage(userId, userEmail, userName, DateTime.UtcNow, habits, completions, goals, reviews, notifications);
            var json = JsonSerializer.Serialize(package, new JsonSerializerOptions { WriteIndented = true });
            bytes = Encoding.UTF8.GetBytes(json);
            contentType = "application/json; charset=utf-8";
            fileName = $"habitflow-export-{DateTime.UtcNow:yyyyMMddHHmmss}.json";
        }

        var record = new DataExportRequestRecord(
            Guid.NewGuid(), clientId, userId, "User", format, "Completed", fileName, totalRecords, DateTime.UtcNow);
        await portabilityRepo.RecordExportAsync(record, ct);
        await audit.LogAsync("data_export.requested", "Solicitação de exportação de dados", AuditSeverity.Info, userId, userEmail, new { format, totalRecords, clientId }, ct);
        await audit.LogAsync("data_export.completed", "Exportação de dados concluída", AuditSeverity.Info, userId, userEmail, new { format, fileName, clientId }, ct);

        return (bytes, contentType, fileName);
    }

    public async Task<ImportSimulationResult> SimulateHabitsCsvAsync(
        Guid clientId,
        Guid userId,
        Stream csvStream,
        CancellationToken ct = default)
    {
        using var reader = new StreamReader(csvStream, Encoding.UTF8);
        var lines = new List<string>();
        string? line;
        while ((line = await reader.ReadLineAsync(ct)) != null)
        {
            if (!string.IsNullOrWhiteSpace(line)) lines.Add(line.Trim());
        }

        if (lines.Count <= 1)
        {
            return new ImportSimulationResult(0, 0, 0, ["O arquivo CSV está vazio ou contém apenas o cabeçalho."], []);
        }

        var existingHabits = await habitRepo.ListAsync(clientId, userId, ct);
        var existingNames = new HashSet<string>(existingHabits.Select(h => h.Name.Trim()), StringComparer.OrdinalIgnoreCase);

        var validItems = new List<HabitImportRow>();
        var errors = new List<string>();
        var seenInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Pular cabeçalho
        for (var i = 1; i < lines.Count; i++)
        {
            var rowNum = i + 1;
            var parts = ParseCsvLine(lines[i]);
            if (parts.Length < 1 || string.IsNullOrWhiteSpace(parts[0]))
            {
                errors.Add($"Linha {rowNum}: Nome do hábito é obrigatório.");
                continue;
            }

            var name = parts[0].Trim();
            if (name.Length is < 2 or > 100)
            {
                errors.Add($"Linha {rowNum}: Nome '{name}' deve ter entre 2 e 100 caracteres.");
                continue;
            }

            if (seenInFile.Contains(name))
            {
                errors.Add($"Linha {rowNum}: Hábito '{name}' duplicado dentro do arquivo.");
                continue;
            }
            seenInFile.Add(name);

            if (existingNames.Contains(name))
            {
                errors.Add($"Linha {rowNum}: Você já possui um hábito com o nome '{name}'.");
                continue;
            }

            var category = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]) ? parts[1].Trim() : "Geral";
            var frequency = parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2]) ? parts[2].Trim() : "Daily";
            var description = parts.Length > 3 && !string.IsNullOrWhiteSpace(parts[3]) ? parts[3].Trim() : null;
            var targetDays = 7;
            if (parts.Length > 4 && int.TryParse(parts[4], out var td) && td is >= 1 and <= 7)
            {
                targetDays = td;
            }

            validItems.Add(new HabitImportRow(name, category, frequency, description, targetDays));
        }

        return new ImportSimulationResult(lines.Count - 1, validItems.Count, errors.Count, errors, validItems);
    }

    public async Task<Result<int>> ExecuteHabitsImportAsync(
        Guid clientId,
        Guid userId,
        IReadOnlyList<HabitImportRow> items,
        CancellationToken ct = default)
    {
        if (items.Count == 0) return Result<int>.Failure("import.empty", "Nenhum item válido para importar.");

        var existingHabits = await habitRepo.ListAsync(clientId, userId, ct);
        var canCreate = await entitlements.CanCreateHabitAsync(userId, existingHabits.Count + items.Count, ct);
        if (!canCreate)
        {
            return Result<int>.Failure("plan_limit_reached", "A importação ultrapassa o limite de hábitos ativos do seu plano atual.");
        }

        var batchId = Guid.NewGuid();
        await audit.LogAsync("data_import.started", "Início da importação de hábitos", AuditSeverity.Info, userId, null, new { batchId, count = items.Count, clientId }, ct);

        var imported = 0;
        var errors = new List<string>();

        foreach (var item in items)
        {
            try
            {
                var freq = Enum.TryParse<HabitFrequencyType>(item.Frequency, true, out var f) ? f : HabitFrequencyType.Daily;
                var habit = new Habit(
                    Guid.NewGuid(),
                    userId,
                    item.Name,
                    "#10B981",
                    item.Category,
                    false,
                    null,
                    DateTime.UtcNow,
                    DateTime.UtcNow,
                    freq,
                    item.TargetDaysPerWeek,
                    null,
                    item.Description,
                    0,
                    clientId
                );
                await habitRepo.CreateAsync(habit, ct);
                imported++;
            }
            catch (Exception ex)
            {
                errors.Add($"Erro ao importar '{item.Name}': {ex.Message}");
            }
        }

        var batchRecord = new DataImportBatchRecord(
            batchId,
            clientId,
            userId,
            "habits",
            "csv",
            errors.Count == 0 ? "Completed" : "CompletedWithErrors",
            items.Count,
            imported,
            errors.Count,
            JsonSerializer.Serialize(errors),
            DateTime.UtcNow
        );

        await portabilityRepo.RecordImportBatchAsync(batchRecord, ct);
        await audit.LogAsync("data_import.completed", "Importação de hábitos concluída", AuditSeverity.Info, userId, null, new { batchId, imported, failed = errors.Count, clientId }, ct);

        return Result<int>.Success(imported);
    }

    private static string EscapeCsv(string? value) =>
        (value ?? string.Empty).Replace("\"", "\"\"");

    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '\"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                {
                    current.Append('\"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if ((c == ',' || c == ';') && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        result.Add(current.ToString());
        return result.ToArray();
    }
}
