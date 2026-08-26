namespace HabitFlow.Domain;

public enum OrganizationRole { Owner, Admin, TeamManager, Member, ReportReader }
public enum InvitationStatus { Pending, Accepted, Declined, Cancelled, Expired }
public enum CorporateProgramStatus { Draft, Active, Paused, Ended, Archived }
public enum TeamChallengeStatus { Draft, Active, Finished, Cancelled, Archived }

public sealed record OrganizationMember(Guid ClientId, Guid UserId, OrganizationRole Role, bool IsActive);
public sealed record Team(Guid Id, Guid ClientId, string Name, string? Description, bool IsArchived, DateTime CreatedAt, DateTime UpdatedAt);
public sealed record TeamMember(Guid ClientId, Guid TeamId, Guid UserId, bool IsManager, DateTime JoinedAt);
public sealed record TeamInvitation(Guid Id, Guid ClientId, Guid? TeamId, string Email, OrganizationRole Role, string TokenHash, InvitationStatus Status, DateTime SentAt, DateTime ExpiresAt, DateTime? RespondedAt);
public sealed record CorporateProgram(Guid Id, Guid ClientId, string Name, string Description, string Objective, DateOnly StartsOn, DateOnly EndsOn, string Audience, CorporateProgramStatus Status, Guid OwnerUserId, bool AllowLeaving, DateTime CreatedAt, DateTime UpdatedAt);
public sealed record CorporateProgramMember(Guid ClientId, Guid ProgramId, Guid UserId, DateTime JoinedAt, DateTime? LeftAt);
public sealed record TeamChallenge(Guid Id, Guid ClientId, Guid TeamId, Guid? ProgramId, string Name, string Goal, DateOnly StartsOn, DateOnly EndsOn, int Target, bool IsCollective, bool RankingEnabled, TeamChallengeStatus Status, DateTime CreatedAt);
public sealed record PrivacyPreference(Guid ClientId, Guid UserId, bool HabitsPrivate, bool ShareProgramProgress, DateTime UpdatedAt);
public sealed record CorporateAuditEvent(Guid EventId, string Code, string CorrelationId, Guid ClientId, Guid ActorUserId, string Result, DateTime OccurredAt);

public sealed record AggregateCorporateReport(int EligibleMembers, int Participants, int CompletedActions, int PossibleActions)
{
    public decimal? ParticipationRate => EligibleMembers == 0 ? null : Math.Round((decimal)Participants / EligibleMembers * 100, 1);
    public decimal? CompletionRate => PossibleActions == 0 ? null : Math.Round((decimal)CompletedActions / PossibleActions * 100, 1);
}
