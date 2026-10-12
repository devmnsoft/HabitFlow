using HabitFlow.Application;
using HabitFlow.Domain;

namespace HabitFlow.Web.Models;

public sealed record RoutineSessionPrepareViewModel(
    string Name,
    int? AvailableMinutes,
    RoutineSessionProposal Proposal,
    RoutineSessionDetails? ActiveSession);

public sealed record RoutineSessionDetailsViewModel(RoutineSessionDetails Details, string Message);

public sealed record RoutineSessionHistoryViewModel(
    IReadOnlyList<RoutineSessionSummary> Sessions,
    DateOnly? From,
    DateOnly? To,
    string? Status,
    int Page);

public sealed class PrepareRoutineSessionForm
{
    public string? Name { get; set; }
    public int? AvailableMinutes { get; set; }
    public Guid[] HabitIds { get; set; } = [];
    public string? IdempotencyKey { get; set; }
}

public sealed class RoutineSessionCommandForm
{
    public string? IdempotencyKey { get; set; }
}

public sealed class RoutineSessionItemForm
{
    public decimal? Quantity { get; set; }
    public string? Unit { get; set; }
    public int? EffectiveMinutes { get; set; }
    public string? IdempotencyKey { get; set; }
    public string? Reason { get; set; }
}

public sealed class CloseRoutineSessionForm
{
    public string? Reflection { get; set; }
    public string? IdempotencyKey { get; set; }
}
