using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
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
    private const int MaxImportBytes = 512 * 1024;
    private const int MaxImportRows = 500;
    private static readonly string[] CsvHeaders = ["nome", "categoria", "frequencia", "descricao", "meta_dias_semana"];

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
        if (csvStream.CanSeek && csvStream.Length > MaxImportBytes)
            return EmptySimulation(["O arquivo CSV excede o limite de 512 KB."]);

        using var reader = new StreamReader(csvStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: false);
        var lines = new List<string>();
        string? line;
        while ((line = await reader.ReadLineAsync(ct)) != null)
        {
            if (!string.IsNullOrWhiteSpace(line)) lines.Add(line.Trim());
            if (lines.Count > MaxImportRows + 1)
                return EmptySimulation([$"O arquivo excede o limite de {MaxImportRows} linhas de dados."]);
        }

        if (lines.Count <= 1)
        {
            return EmptySimulation(["O arquivo CSV está vazio ou contém apenas o cabeçalho."]);
        }

        var header = ParseCsvLine(lines[0]).Select(NormalizeHeader).ToArray();
        if (!CsvHeaders.All(expected => header.Contains(expected, StringComparer.Ordinal)))
            return EmptySimulation([$"Cabeçalho inválido. Use exatamente: {string.Join(",", CsvHeaders)}."]);

        var existingHabits = await habitRepo.ListAsync(clientId, userId, ct);
        var existingByName = existingHabits
            .Where(h => !h.IsArchived)
            .GroupBy(h => h.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var validItems = new List<HabitImportRow>();
        var errors = new List<string>();
        var seenInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var batchToken = CreateBatchToken(clientId, userId, lines);

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
            if (LooksLikeSpreadsheetFormula(name))
            {
                errors.Add($"Linha {rowNum}: Nome do hábito começa com caractere perigoso para planilhas.");
                continue;
            }

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

            var category = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]) ? parts[1].Trim() : "Geral";
            var frequency = parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2]) ? parts[2].Trim() : "Daily";
            var description = parts.Length > 3 && !string.IsNullOrWhiteSpace(parts[3]) ? parts[3].Trim() : null;
            if (category.Length > 80)
            {
                errors.Add($"Linha {rowNum}: Categoria deve ter até 80 caracteres.");
                continue;
            }
            if (!Enum.TryParse<HabitFrequencyType>(frequency, true, out var parsedFrequency))
            {
                errors.Add($"Linha {rowNum}: Frequência '{frequency}' não é compatível.");
                continue;
            }

            var targetDays = 7;
            if (parts.Length > 4 && !string.IsNullOrWhiteSpace(parts[4]) && (!int.TryParse(parts[4], out targetDays) || targetDays is < 1 or > 7))
            {
                errors.Add($"Linha {rowNum}: meta_dias_semana deve ser um número entre 1 e 7.");
                continue;
            }

            var warnings = new List<string>();
            var decision = "create";
            Guid? existingHabitId = null;
            DateTime? expectedUpdatedAt = null;
            if (existingByName.TryGetValue(name, out var existing))
            {
                decision = "ignore";
                existingHabitId = existing.Id;
                expectedUpdatedAt = existing.UpdatedAt;
                warnings.Add($"Correspondência exata encontrada em '{existing.Name}'. Escolha ignorar, criar novo ou atualizar campos revisados.");
            }

            validItems.Add(new HabitImportRow(name, category, parsedFrequency.ToString(), description, targetDays, decision,
                existingHabitId, expectedUpdatedAt, batchToken, rowNum, warnings));
        }

        return new ImportSimulationResult(batchToken, lines.Count - 1, validItems.Count, errors.Count, errors, validItems);
    }

    public async Task<Result<int>> ExecuteHabitsImportAsync(
        Guid clientId,
        Guid userId,
        IReadOnlyList<HabitImportRow> items,
        CancellationToken ct = default)
    {
        if (items.Count == 0) return Result<int>.Failure("import.empty", "Nenhum item válido para importar.");
        if (items.Count > MaxImportRows) return Result<int>.Failure("import.too_many_rows", $"Importe no máximo {MaxImportRows} hábitos por lote.");
        if (items.Any(item => item.BatchToken is null || item.RowNumber <= 1))
            return Result<int>.Failure("import.preview_required", "Revise a prévia antes de confirmar a importação.");

        var existingHabits = await habitRepo.ListAsync(clientId, userId, ct);
        var existingById = existingHabits.ToDictionary(h => h.Id);
        var existingNames = existingHabits.Where(h => !h.IsArchived).Select(h => h.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var createItems = items.Where(item => IsCreateDecision(item.Decision)).ToList();
        var createCount = 0;
        foreach (var item in createItems)
        {
            if (await habitRepo.FindByIdempotencyKeyAsync(clientId, userId, IdempotencyKey(item), ct) is null)
                createCount++;
        }
        var activeHabitCount = existingHabits.Count(h => !h.IsArchived);
        for (var i = 0; i < createCount; i++)
            if (!await entitlements.CanCreateHabitAsync(userId, activeHabitCount + i, ct))
                return Result<int>.Failure("plan_limit_reached", "A importação ultrapassa o limite de hábitos ativos do seu plano atual.");

        var batchId = Guid.NewGuid();
        await audit.LogAsync("data_import.started", "Início da importação de hábitos", AuditSeverity.Info, userId, null, new { batchId, count = items.Count, clientId }, ct);

        var imported = 0;
        var updated = 0;
        var ignored = 0;
        var errors = new List<string>();

        foreach (var item in items)
        {
            try
            {
                if (!ValidateImportItem(item, errors)) continue;
                if (IsIgnoreDecision(item.Decision))
                {
                    ignored++;
                    continue;
                }

                var freq = Enum.TryParse<HabitFrequencyType>(item.Frequency, true, out var f) ? f : HabitFrequencyType.Daily;
                if (IsUpdateDecision(item.Decision))
                {
                    if (item.ExistingHabitId is not { } existingHabitId || !existingById.TryGetValue(existingHabitId, out var existing) || existing.ClientId != clientId || existing.UserId != userId)
                    {
                        errors.Add($"Linha {item.RowNumber}: hábito existente não pertence a esta conta.");
                        continue;
                    }
                    if (item.ExpectedUpdatedAt.HasValue && existing.UpdatedAt != item.ExpectedUpdatedAt.Value)
                    {
                        errors.Add($"Linha {item.RowNumber}: hábito '{existing.Name}' mudou desde a prévia. Revise novamente antes de atualizar.");
                        continue;
                    }
                    if (existingHabits.Any(h => h.Id != existing.Id && !h.IsArchived && h.Name.Equals(item.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
                    {
                        errors.Add($"Linha {item.RowNumber}: outro hábito ativo já usa o nome '{item.Name}'.");
                        continue;
                    }
                    var updatedHabit = existing with
                    {
                        Name = item.Name.Trim(),
                        Category = item.Category.Trim(),
                        FrequencyType = freq,
                        TargetPerWeek = item.TargetDaysPerWeek,
                        Notes = item.Description,
                        UpdatedAt = DateTime.UtcNow
                    };
                    if (await habitRepo.UpdateAsync(clientId, userId, updatedHabit, ct))
                        updated++;
                    else
                        errors.Add($"Linha {item.RowNumber}: não foi possível atualizar '{existing.Name}'.");
                    continue;
                }

                var idempotencyKey = IdempotencyKey(item);
                if (await habitRepo.FindByIdempotencyKeyAsync(clientId, userId, idempotencyKey, ct) is not null)
                {
                    ignored++;
                    continue;
                }
                if (existingNames.Contains(item.Name.Trim()) && !IsCreateNewDecision(item.Decision))
                {
                    errors.Add($"Linha {item.RowNumber}: já existe um hábito com o nome '{item.Name}'. Escolha criar novo, ignorar ou atualizar.");
                    continue;
                }
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
                    clientId,
                    TemplateIdempotencyKey: idempotencyKey
                );
                await habitRepo.CreateAsync(habit, ct);
                existingNames.Add(habit.Name);
                imported++;
            }
            catch (Exception ex)
            {
                errors.Add($"Linha {item.RowNumber}: erro ao importar '{item.Name}': {ex.Message}");
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
            imported + updated + ignored,
            errors.Count,
            JsonSerializer.Serialize(errors),
            DateTime.UtcNow
        );

        await portabilityRepo.RecordImportBatchAsync(batchRecord, ct);
        await audit.LogAsync("data_import.completed", "Importação de hábitos concluída", AuditSeverity.Info, userId, null, new { batchId, imported, updated, ignored, failed = errors.Count, clientId }, ct);

        return errors.Count == items.Count
            ? Result<int>.Failure("import.no_rows_applied", "Nenhuma linha pôde ser aplicada.")
            : Result<int>.Success(imported + updated);
    }

    private static string EscapeCsv(string? value) =>
        (value ?? string.Empty).Replace("\"", "\"\"");

    private static ImportSimulationResult EmptySimulation(IReadOnlyList<string> errors) =>
        new(Guid.NewGuid(), 0, 0, errors.Count, errors, []);

    private static string NormalizeHeader(string value) => value.Trim().Trim('\uFEFF').ToLowerInvariant();

    private static bool LooksLikeSpreadsheetFormula(string value) =>
        value.Length > 0 && value[0] is '=' or '+' or '-' or '@';

    private static bool IsIgnoreDecision(string? decision) => string.Equals(decision, "ignore", StringComparison.OrdinalIgnoreCase);
    private static bool IsUpdateDecision(string? decision) => string.Equals(decision, "update", StringComparison.OrdinalIgnoreCase);
    private static bool IsCreateNewDecision(string? decision) => string.Equals(decision, "create_new", StringComparison.OrdinalIgnoreCase);
    private static bool IsCreateDecision(string? decision) => string.Equals(decision, "create", StringComparison.OrdinalIgnoreCase) || IsCreateNewDecision(decision);

    private static bool ValidateImportItem(HabitImportRow item, ICollection<string> errors)
    {
        if (item.Name.Trim().Length is < 2 or > 100 || LooksLikeSpreadsheetFormula(item.Name.Trim()))
        {
            errors.Add($"Linha {item.RowNumber}: nome do hábito inválido.");
            return false;
        }
        if (item.Category.Trim().Length > 80)
        {
            errors.Add($"Linha {item.RowNumber}: categoria deve ter até 80 caracteres.");
            return false;
        }
        if (!Enum.TryParse<HabitFrequencyType>(item.Frequency, true, out _) || item.TargetDaysPerWeek is < 1 or > 7)
        {
            errors.Add($"Linha {item.RowNumber}: frequência ou meta semanal incompatível.");
            return false;
        }
        if (!IsCreateDecision(item.Decision) && !IsUpdateDecision(item.Decision) && !IsIgnoreDecision(item.Decision))
        {
            errors.Add($"Linha {item.RowNumber}: decisão de importação inválida.");
            return false;
        }
        return true;
    }

    private static Guid CreateBatchToken(Guid clientId, Guid userId, IReadOnlyList<string> lines)
    {
        var payload = $"{clientId:N}:{userId:N}:{string.Join('\n', lines)}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return new Guid(hash[..16]);
    }

    private static Guid IdempotencyKey(HabitImportRow item)
    {
        var payload = $"{item.BatchToken:N}:{item.RowNumber}:{item.Name.Trim().ToUpperInvariant()}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return new Guid(hash[..16]);
    }

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
