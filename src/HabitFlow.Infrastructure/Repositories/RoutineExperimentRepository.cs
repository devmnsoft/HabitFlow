using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class RoutineExperimentRepository(SqlExecutor db) : IRoutineExperimentRepository
{
    private const string ExperimentColumns = """
        id as Id, client_id as ClientId, user_id as UserId, habit_id as HabitId, name as Name,
        difficulty as Difficulty, adjustment_type as AdjustmentType, proposed_adjustment as ProposedAdjustment,
        test_start as TestStart, test_end as TestEnd, baseline_start as BaselineStart, baseline_end as BaselineEnd,
        primary_indicator as PrimaryIndicator, status as Status, reflection as Reflection,
        previous_configuration_json as PreviousConfigurationJson, proposed_configuration_json as ProposedConfigurationJson,
        decision as Decision, version as Version, created_at as CreatedAt, updated_at as UpdatedAt,
        started_at as StartedAt, closed_at as ClosedAt
        """;

    public async Task<RoutineExperiment?> GetAsync(Guid clientId, Guid userId, Guid id, CancellationToken ct = default)
    {
        var row = await db.QuerySingleOrDefaultAsync<RoutineExperimentRow>($"select {ExperimentColumns} from habitflow.routine_experiments where client_id=@clientId and user_id=@userId and id=@id", new { clientId, userId, id }, ct);
        return row is null ? null : Map(row);
    }

    public async Task<IReadOnlyList<RoutineExperiment>> ListByHabitAsync(Guid clientId, Guid userId, Guid habitId, CancellationToken ct = default)
    {
        var rows = await db.QueryAsync<RoutineExperimentRow>($"select {ExperimentColumns} from habitflow.routine_experiments where client_id=@clientId and user_id=@userId and habit_id=@habitId order by test_start desc, created_at desc", new { clientId, userId, habitId }, ct);
        return rows.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<RoutineExperiment>> ListActiveForHabitAsync(Guid clientId, Guid userId, Guid habitId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var rows = await db.QueryAsync<RoutineExperimentRow>($"select {ExperimentColumns} from habitflow.routine_experiments where client_id=@clientId and user_id=@userId and habit_id=@habitId and status='Active' and daterange(test_start,test_end,'[]') && daterange(@from,@to,'[]')", new { clientId, userId, habitId, from, to }, ct);
        return rows.Select(Map).ToList();
    }

    public Task CreateAsync(RoutineExperiment e, CancellationToken ct = default) => db.ExecuteAsync("""
        insert into habitflow.routine_experiments(id,client_id,user_id,habit_id,name,difficulty,adjustment_type,proposed_adjustment,test_start,test_end,baseline_start,baseline_end,primary_indicator,status,reflection,previous_configuration_json,proposed_configuration_json,decision,version,created_at,updated_at,started_at,closed_at)
        select @Id,@ClientId,@UserId,h.id,@Name,@Difficulty,@AdjustmentType,@ProposedAdjustment,@TestStart,@TestEnd,@BaselineStart,@BaselineEnd,@PrimaryIndicator,@Status,@Reflection,@PreviousConfigurationJson::jsonb,@ProposedConfigurationJson::jsonb,@Decision,@Version,@CreatedAt,@UpdatedAt,@StartedAt,@ClosedAt
        from habitflow.habits h where h.id=@HabitId and h.client_id=@ClientId and h.user_id=@UserId
        """, ToDb(e), ct);

    public async Task<bool> UpdateAsync(RoutineExperiment e, int expectedVersion, CancellationToken ct = default) =>
        await db.ExecuteAsync("""
            update habitflow.routine_experiments
            set status=@Status, reflection=@Reflection, decision=@Decision, version=version+1, updated_at=@UpdatedAt, started_at=@StartedAt, closed_at=@ClosedAt
            where id=@Id and client_id=@ClientId and user_id=@UserId and version=@expectedVersion
            """, ToDb(e, expectedVersion), ct) == 1;

    public Task AddEventAsync(RoutineExperimentEvent value, CancellationToken ct = default) => db.ExecuteAsync("""
        insert into habitflow.routine_experiment_events(id,client_id,user_id,experiment_id,event_type,message,created_at)
        select @Id,@ClientId,@UserId,e.id,@EventType,@Message,@CreatedAt from habitflow.routine_experiments e
        where e.id=@ExperimentId and e.client_id=@ClientId and e.user_id=@UserId
        """, value, ct);

    public Task AddLearningAsync(RoutineExperimentLearning value, CancellationToken ct = default) => db.ExecuteAsync("""
        insert into habitflow.routine_experiment_learnings(id,client_id,user_id,habit_id,experiment_id,outcome,observation,is_private,created_at)
        select @Id,@ClientId,@UserId,e.habit_id,e.id,@Outcome,@Observation,@IsPrivate,@CreatedAt from habitflow.routine_experiments e
        where e.id=@ExperimentId and e.client_id=@ClientId and e.user_id=@UserId and e.habit_id=@HabitId
        """, new { value.Id, value.ClientId, value.UserId, value.HabitId, value.ExperimentId, Outcome = value.Outcome.ToString(), value.Observation, value.IsPrivate, value.CreatedAt }, ct);

    public async Task<IReadOnlyList<RoutineExperimentLearning>> ListLearningsByHabitAsync(Guid clientId, Guid userId, Guid habitId, CancellationToken ct = default)
    {
        var rows = await db.QueryAsync<RoutineExperimentLearningRow>("""
            select id as Id, client_id as ClientId, user_id as UserId, habit_id as HabitId, experiment_id as ExperimentId,
                   outcome as Outcome, observation as Observation, is_private as IsPrivate, created_at as CreatedAt
            from habitflow.routine_experiment_learnings
            where client_id=@clientId and user_id=@userId and habit_id=@habitId
            order by created_at desc
            """, new { clientId, userId, habitId }, ct);
        return rows.Select(x => new RoutineExperimentLearning(x.Id, x.ClientId, x.UserId, x.HabitId, x.ExperimentId, Enum.Parse<RoutineExperimentLearningOutcome>(x.Outcome), x.Observation, x.IsPrivate, ToUtcOffset(x.CreatedAt))).ToList();
    }

    private static object ToDb(RoutineExperiment e, int? expectedVersion = null) => new
    {
        e.Id, e.ClientId, e.UserId, e.HabitId, e.Name, e.Difficulty,
        AdjustmentType = e.AdjustmentType.ToString(), e.ProposedAdjustment, e.TestStart, e.TestEnd, e.BaselineStart, e.BaselineEnd,
        PrimaryIndicator = e.PrimaryIndicator.ToString(), Status = e.Status.ToString(), e.Reflection,
        e.PreviousConfigurationJson, e.ProposedConfigurationJson, Decision = e.Decision.ToString(),
        e.Version, e.CreatedAt, e.UpdatedAt, e.StartedAt, e.ClosedAt, expectedVersion
    };

    private static RoutineExperiment Map(RoutineExperimentRow r) => new(r.Id, r.ClientId, r.UserId, r.HabitId, r.Name, r.Difficulty,
        Enum.Parse<RoutineExperimentAdjustmentType>(r.AdjustmentType), r.ProposedAdjustment, r.TestStart, r.TestEnd,
        r.BaselineStart, r.BaselineEnd, Enum.Parse<RoutineExperimentIndicator>(r.PrimaryIndicator),
        Enum.Parse<RoutineExperimentStatus>(r.Status), r.Reflection, r.PreviousConfigurationJson, r.ProposedConfigurationJson,
        Enum.Parse<RoutineExperimentDecision>(r.Decision), r.Version, ToUtcOffset(r.CreatedAt), ToUtcOffset(r.UpdatedAt),
        ToUtcOffset(r.StartedAt), ToUtcOffset(r.ClosedAt));

    private static DateTimeOffset ToUtcOffset(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        return new DateTimeOffset(utc);
    }
    private static DateTimeOffset? ToUtcOffset(DateTime? value) => value.HasValue ? ToUtcOffset(value.Value) : null;

    private sealed class RoutineExperimentRow
    {
        public Guid Id { get; init; }
        public Guid ClientId { get; init; }
        public Guid UserId { get; init; }
        public Guid HabitId { get; init; }
        public string Name { get; init; } = "";
        public string Difficulty { get; init; } = "";
        public string AdjustmentType { get; init; } = "";
        public string ProposedAdjustment { get; init; } = "";
        public DateOnly TestStart { get; init; }
        public DateOnly TestEnd { get; init; }
        public DateOnly BaselineStart { get; init; }
        public DateOnly BaselineEnd { get; init; }
        public string PrimaryIndicator { get; init; } = "";
        public string Status { get; init; } = "";
        public string? Reflection { get; init; }
        public string PreviousConfigurationJson { get; init; } = "{}";
        public string ProposedConfigurationJson { get; init; } = "{}";
        public string Decision { get; init; } = "";
        public int Version { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
        public DateTime? StartedAt { get; init; }
        public DateTime? ClosedAt { get; init; }
    }

    private sealed class RoutineExperimentLearningRow
    {
        public Guid Id { get; init; }
        public Guid ClientId { get; init; }
        public Guid UserId { get; init; }
        public Guid HabitId { get; init; }
        public Guid ExperimentId { get; init; }
        public string Outcome { get; init; } = "";
        public string Observation { get; init; } = "";
        public bool IsPrivate { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}
