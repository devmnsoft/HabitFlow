namespace HabitFlow.Domain;

public interface IHabitJourneyRepository
{
    Task<IReadOnlyList<HabitJourney>> ListAvailableAsync(Guid clientId, CancellationToken ct = default);
    Task<HabitJourneyDetails?> GetDetailsAsync(Guid journeyId, Guid clientId, Guid userId, CancellationToken ct = default);
    Task<HabitJourneyMember?> GetMembershipAsync(Guid journeyId, Guid clientId, Guid userId, CancellationToken ct = default);
    Task JoinAsync(HabitJourneyMember member, CancellationToken ct = default);
    Task LeaveAsync(Guid journeyId, Guid clientId, Guid userId, CancellationToken ct = default);
}
