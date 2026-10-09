using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class HabitJourneyRepository(SqlExecutor db) : IHabitJourneyRepository
{
    public async Task<IReadOnlyList<HabitJourney>> ListAvailableAsync(Guid clientId, CancellationToken ct = default)
    {
        var rows = await db.QueryAsync<HabitJourneyRow>("""
            select id as "Id", client_id as "ClientId", name as "Name", description as "Description",
                   category as "Category", goal as "Goal", suggested_duration_days as "SuggestedDurationDays",
                   frequency as "Frequency", difficulty as "Difficulty", coalesce(tags, '') as "Tags",
                   target_audience as "TargetAudience", minimum_plan as "MinimumPlan", is_active as "IsActive",
                   is_official as "IsOfficial", created_at as "CreatedAt", updated_at as "UpdatedAt"
            from habitflow.habit_journeys
            where is_active = true and (client_id is null or client_id = @clientId)
            order by is_official desc, category, name
            """, new { clientId }, ct);
        return rows.Select(Map).ToList();
    }

    public async Task<HabitJourneyDetails?> GetDetailsAsync(Guid journeyId, Guid clientId, Guid userId, CancellationToken ct = default)
    {
        var journey = (await db.QueryAsync<HabitJourneyRow>("""
            select id as "Id", client_id as "ClientId", name as "Name", description as "Description",
                   category as "Category", goal as "Goal", suggested_duration_days as "SuggestedDurationDays",
                   frequency as "Frequency", difficulty as "Difficulty", coalesce(tags, '') as "Tags",
                   target_audience as "TargetAudience", minimum_plan as "MinimumPlan", is_active as "IsActive",
                   is_official as "IsOfficial", created_at as "CreatedAt", updated_at as "UpdatedAt"
            from habitflow.habit_journeys
            where id = @journeyId and (client_id is null or client_id = @clientId)
            """, new { journeyId, clientId }, ct)).Select(Map).FirstOrDefault();
        if (journey is null) return null;

        var steps = (await db.QueryAsync<HabitJourneyStepRow>("""
            select id as "Id", journey_id as "JourneyId", step_order as "StepOrder", title as "Title",
                   description as "Description", suggested_habit_name as "SuggestedHabitName",
                   suggested_frequency as "SuggestedFrequency", suggested_reminder_time as "SuggestedReminderTime"
            from habitflow.habit_journey_steps
            where journey_id = @journeyId
            order by step_order
            """, new { journeyId }, ct)).Select(MapStep).ToList();
        var membership = await GetMembershipAsync(journeyId, clientId, userId, ct);
        return new(journey, steps, membership);
    }

    public async Task<HabitJourneyMember?> GetMembershipAsync(Guid journeyId, Guid clientId, Guid userId, CancellationToken ct = default)
    {
        var row = await db.QuerySingleOrDefaultAsync<HabitJourneyMemberRow>("""
            select id as "Id", journey_id as "JourneyId", client_id as "ClientId", user_id as "UserId",
                   status as "Status", progress_percentage as "ProgressPercentage", joined_at as "JoinedAt",
                   completed_at as "CompletedAt"
            from habitflow.habit_journey_members
            where journey_id = @journeyId and client_id = @clientId and user_id = @userId
            """, new { journeyId, clientId, userId }, ct);
        return row is null ? null : new(row.Id, row.JourneyId, row.ClientId, row.UserId, row.Status, row.ProgressPercentage, row.JoinedAt, row.CompletedAt);
    }

    public Task JoinAsync(HabitJourneyMember member, CancellationToken ct = default) =>
        db.ExecuteAsync("""
            insert into habitflow.habit_journey_members(id, journey_id, client_id, user_id, status, progress_percentage, joined_at, completed_at)
            values(@Id, @JourneyId, @ClientId, @UserId, @Status, @ProgressPercentage, @JoinedAt, @CompletedAt)
            on conflict (journey_id, user_id) do update set status = 'Active', completed_at = null
            """, member, ct);

    public Task LeaveAsync(Guid journeyId, Guid clientId, Guid userId, CancellationToken ct = default) =>
        db.ExecuteAsync("""
            update habitflow.habit_journey_members
               set status = 'Left'
             where journey_id = @journeyId and client_id = @clientId and user_id = @userId
            """, new { journeyId, clientId, userId }, ct);

    private static HabitJourney Map(HabitJourneyRow row) => new(row.Id, row.ClientId, row.Name, row.Description, row.Category, row.Goal,
        row.SuggestedDurationDays, row.Frequency, row.Difficulty, SplitTags(row.Tags), row.TargetAudience, row.MinimumPlan, row.IsActive,
        row.IsOfficial, row.CreatedAt, row.UpdatedAt);

    private static HabitJourneyStep MapStep(HabitJourneyStepRow row) => new(row.Id, row.JourneyId, row.StepOrder, row.Title, row.Description,
        row.SuggestedHabitName, row.SuggestedFrequency, TimeOnly.TryParse(row.SuggestedReminderTime, out var time) ? time : null);

    private static string[] SplitTags(string? tags) => (tags ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private sealed class HabitJourneyRow
    {
        public Guid Id { get; init; }
        public Guid? ClientId { get; init; }
        public string Name { get; init; } = "";
        public string Description { get; init; } = "";
        public string Category { get; init; } = "";
        public string Goal { get; init; } = "";
        public int SuggestedDurationDays { get; init; }
        public string Frequency { get; init; } = "";
        public string Difficulty { get; init; } = "";
        public string Tags { get; init; } = "";
        public string? TargetAudience { get; init; }
        public string MinimumPlan { get; init; } = PlanCodes.Free;
        public bool IsActive { get; init; }
        public bool IsOfficial { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }

    private sealed class HabitJourneyStepRow
    {
        public Guid Id { get; init; }
        public Guid JourneyId { get; init; }
        public int StepOrder { get; init; }
        public string Title { get; init; } = "";
        public string Description { get; init; } = "";
        public string SuggestedHabitName { get; init; } = "";
        public string SuggestedFrequency { get; init; } = "";
        public string? SuggestedReminderTime { get; init; }
    }

    private sealed class HabitJourneyMemberRow
    {
        public Guid Id { get; init; }
        public Guid JourneyId { get; init; }
        public Guid ClientId { get; init; }
        public Guid UserId { get; init; }
        public string Status { get; init; } = "";
        public decimal ProgressPercentage { get; init; }
        public DateTime JoinedAt { get; init; }
        public DateTime? CompletedAt { get; init; }
    }
}
