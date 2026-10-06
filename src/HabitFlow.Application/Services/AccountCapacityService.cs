using HabitFlow.Domain;

namespace HabitFlow.Application;

public sealed class AccountCapacityService(IUserInviteRepository invites, PlanEntitlementService plans)
{
    public async Task<AccountCapacityUsage> GetUsageAsync(Guid clientId, CancellationToken ct = default)
    {
        var occupied = await GetOccupiedSlotsAsync(clientId, ct);
        var limit = await plans.GetUsersLimitAsync(clientId, ct);
        return new(occupied, limit, await plans.GetInviteBlockAsync(clientId, occupied, ct));
    }

    public Task<int> GetOccupiedSlotsAsync(Guid clientId, CancellationToken ct = default) =>
        invites.GetOccupiedSlotsAsync(clientId, DateTime.UtcNow, ct);

    public async Task<bool> CanReactivateAsync(Guid clientId, CancellationToken ct = default)
    {
        var usage = await GetUsageAsync(clientId, ct);
        return usage.HasCapacity;
    }
}

public sealed record AccountCapacityUsage(int Occupied, int Limit, PlanInviteBlock? InviteBlock)
{
    public int Remaining => Limit < 0 ? -1 : Math.Max(0, Limit - Occupied);
    public int Excess => Limit < 0 ? 0 : Math.Max(0, Occupied - Limit);
    public bool HasCapacity => Limit == -1 || Occupied < Limit;
}
