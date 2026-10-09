using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed record SupportContact(string CompanyName, string Email, string? WhatsAppUrl, string ButtonText, string BusinessHours);

public static class SupportSla
{
    public static int Hours(string priority, string? plan = null)
    {
        var planNormalized = (plan ?? "").Trim().ToLowerInvariant();
        var isEnterprise = planNormalized.Contains("enterprise");
        var isFree = planNormalized.Contains("free");

        if (isEnterprise)
        {
            return priority switch
            {
                "Critical" => 4,
                "High" => 12,
                "Medium" => 24,
                "Low" => 48,
                _ => 24
            };
        }

        if (isFree)
        {
            return priority switch
            {
                "Critical" => 24,
                "High" => 48,
                "Medium" => 72,
                "Low" => 96,
                _ => 72
            };
        }

        return priority switch
        {
            "Critical" => 8,
            "High" => 24,
            "Medium" => 48,
            "Low" => 72,
            _ => 48
        };
    }

    public static DateTime Calculate(DateTime openedUtc, string priority, string? plan = null)
    {
        var remaining = Hours(priority, plan);
        var cursor = DateTime.SpecifyKind(openedUtc, DateTimeKind.Utc);
        while (remaining > 0)
        {
            cursor = cursor.AddHours(1);
            if (cursor.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
                remaining--;
        }
        return cursor;
    }
}

public sealed class SupportCenterService(
    IAssistanceRepository repository,
    WhatsAppValidator validator,
    ProtocolGenerator protocols,
    AssistantSafetyPolicy safety,
    ILogger<SupportCenterService> logger)
{
    public async Task<SupportContact> ContactAsync(CancellationToken ct)
    {
        var s = await repository.GetSupportSettingsAsync(ct);
        string? url = null;
        if (s.IsActive && !string.IsNullOrWhiteSpace(s.WhatsAppPhone) && validator.Validate(new(true, s.WhatsAppPhone, s.DefaultMessage, s.ButtonText)).IsSuccess)
        {
            var digits = new string(s.WhatsAppPhone.Where(char.IsDigit).ToArray());
            url = $"https://wa.me/{digits}?text={Uri.EscapeDataString(s.DefaultMessage)}";
        }
        return new(s.CompanyName, s.SupportEmail, url, s.ButtonText, s.BusinessHours);
    }

    public Task<IReadOnlyList<SupportTicketDetail>> ListAsync(Guid clientId, Guid userId, bool admin, CancellationToken ct) =>
        repository.ListTicketsAsync(clientId, userId, admin, ct);

    public Task<IReadOnlyList<SupportTicketDetail>> ListGlobalAsync(string? status, string? priority, string? category, string? search, CancellationToken ct) =>
        repository.ListAllTicketsAsync(status, priority, category, search, ct);

    public Task<SupportTicketDetail?> GetAsync(Guid clientId, Guid userId, Guid id, bool admin, CancellationToken ct) =>
        repository.GetTicketAsync(clientId, userId, id, admin, ct);

    public Task<SupportTicketDetail?> GetGlobalAsync(Guid id, CancellationToken ct) =>
        repository.GetTicketByIdGlobalAsync(id, ct);

    public async Task<Guid> CreateAsync(
        Guid clientId, Guid userId, string category, string priority, string subject,
        string description, string route, string browser, string viewport, string plan,
        string correlationId, CancellationToken ct)
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        priority = AllowedPriority(priority);
        var slaDueAt = SupportSla.Calculate(now, priority, plan);
        var safe = $"route={Clean(route, 120)}; browser={Clean(browser, 160)}; viewport={Clean(viewport, 30)}; app=6.21.0; correlation={Clean(correlationId, 80)}; plan={Clean(plan, 30)}; at={now:O}";

        await repository.CreateTicketAsync(new(
            id, clientId, userId, protocols.Generate("SUP"),
            AllowedCategory(category), priority, "Open",
            Clean(subject, 160), safety.Sanitize(description), safe,
            null, slaDueAt, now, now, null, null, null, null, null, null), ct);

        logger.LogInformation(ApplicationEvents.SupportTicketCreated,
            "support.ticket.created CorrelationId={CorrelationId} ClientId={ClientId} UserId={UserId} TicketId={TicketId} Category={Category} Priority={Priority} Plan={Plan} SlaDueAt={SlaDueAt}",
            correlationId, clientId, userId, id, category, priority, plan, slaDueAt);

        return id;
    }

    public async Task<bool> ReplyAsync(Guid clientId, Guid userId, Guid id, bool admin, string message, bool isInternal, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(message) || (isInternal && !admin)) return false;
        var ticket = admin ? await repository.GetTicketByIdGlobalAsync(id, ct) : await repository.GetTicketAsync(clientId, userId, id, admin, ct);
        if (ticket is null || ticket.Status is "Closed" or "Cancelled" or "Canceled") return false;

        await repository.AddTicketMessageAsync(new(Guid.NewGuid(), ticket.ClientId, id, userId, admin, isInternal, safety.Sanitize(message), DateTime.UtcNow), ct);

        logger.LogInformation(ApplicationEvents.SupportTicketUpdated,
            "support.ticket.updated Action=message_added ClientId={ClientId} TicketId={TicketId} IsStaff={IsStaff} IsInternal={IsInternal}",
            ticket.ClientId, id, admin, isInternal);

        return true;
    }

    public async Task<Result> ResolveAsync(Guid clientId, Guid userId, Guid id, string resolutionReason, bool admin, CancellationToken ct)
    {
        var reason = (resolutionReason ?? "").Trim();
        if (reason.Length < 5)
            return Result.Failure("support.reason_required", "O motivo de resolução deve conter pelo menos 5 caracteres.");

        var ticket = admin ? await repository.GetTicketByIdGlobalAsync(id, ct) : await repository.GetTicketAsync(clientId, userId, id, admin, ct);
        if (ticket is null) return Result.Failure("support.ticket_not_found", "Chamado não encontrado.");

        await repository.ResolveTicketAsync(ticket.ClientId, id, userId, safety.Sanitize(reason), ct);

        logger.LogInformation(ApplicationEvents.SupportTicketUpdated,
            "support.ticket.updated Action=resolved ClientId={ClientId} TicketId={TicketId} ActorUserId={ActorUserId}",
            ticket.ClientId, id, userId);

        return Result.Success();
    }

    public async Task<Result> CloseAsync(Guid clientId, Guid userId, Guid id, string? reason, bool admin, CancellationToken ct)
    {
        var ticket = admin ? await repository.GetTicketByIdGlobalAsync(id, ct) : await repository.GetTicketAsync(clientId, userId, id, admin, ct);
        if (ticket is null) return Result.Failure("support.ticket_not_found", "Chamado não encontrado.");

        await repository.UpdateTicketStatusAsync(ticket.ClientId, id, "Closed", DateTime.UtcNow, ct);

        logger.LogInformation(ApplicationEvents.SupportTicketClosed,
            "support.ticket.closed ClientId={ClientId} UserId={UserId} TicketId={TicketId} Result=closed",
            ticket.ClientId, userId, id);

        return Result.Success();
    }

    public async Task<Result> ReopenAsync(Guid clientId, Guid userId, Guid id, string reason, string? plan, CancellationToken ct)
    {
        var text = (reason ?? "").Trim();
        if (text.Length < 5)
            return Result.Failure("support.reason_required", "Informe o motivo da reabertura com pelo menos 5 caracteres.");

        var ticket = await repository.GetTicketAsync(clientId, userId, id, false, ct);
        if (ticket is null) return Result.Failure("support.ticket_not_found", "Chamado não encontrado.");
        if (ticket.Status is not "Resolved" and not "Closed")
            return Result.Failure("support.cannot_reopen", "Apenas chamados resolvidos ou encerrados podem ser reabertos.");

        var newSla = SupportSla.Calculate(DateTime.UtcNow, ticket.Priority, plan);
        await repository.ReopenTicketAsync(clientId, id, userId, safety.Sanitize(text), newSla, ct);

        logger.LogInformation(ApplicationEvents.SupportTicketUpdated,
            "support.ticket.updated Action=reopened ClientId={ClientId} TicketId={TicketId} UserId={UserId}",
            clientId, id, userId);

        return Result.Success();
    }

    public async Task<Result> SubmitSatisfactionAsync(Guid clientId, Guid userId, Guid ticketId, int rating, string? feedback, CancellationToken ct)
    {
        if (rating is < 1 or > 5)
            return Result.Failure("support.invalid_rating", "A nota de satisfação deve ser entre 1 e 5 estrelas.");

        var ticket = await repository.GetTicketAsync(clientId, userId, ticketId, false, ct);
        if (ticket is null) return Result.Failure("support.ticket_not_found", "Chamado não encontrado.");
        if (ticket.Status is not "Resolved" and not "Closed")
            return Result.Failure("support.not_resolved", "A pesquisa de satisfação só está disponível para chamados resolvidos.");

        await repository.SubmitSatisfactionAsync(clientId, ticketId, userId, rating, string.IsNullOrWhiteSpace(feedback) ? null : safety.Sanitize(feedback.Trim()), ct);

        logger.LogInformation("support.satisfaction_submitted ClientId={ClientId} TicketId={TicketId} Rating={Rating}",
            clientId, ticketId, rating);

        return Result.Success();
    }

    public async Task<IReadOnlyList<SupportTicketMessage>> MessagesAsync(Guid clientId, Guid ticketId, bool staff, CancellationToken ct) =>
        (await repository.ListTicketMessagesAsync(clientId, ticketId, ct)).Where(x => staff || !x.IsInternal).ToArray();

    private static string Clean(string? value, int max)
    {
        var v = (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ').Trim();
        return v.Length <= max ? v : v[..max];
    }

    private static string AllowedCategory(string c) =>
        new[] { "Question", "Error", "Billing", "Access", "Configuration", "Suggestion", "Commercial" }.Contains(c) ? c : "Question";

    private static string AllowedPriority(string p) =>
        new[] { "Low", "Medium", "High", "Critical" }.Contains(p) ? p : "Medium";
}
