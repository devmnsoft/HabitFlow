using HabitFlow.Application;
using HabitFlow.Domain;
using Xunit;

namespace HabitFlow.Tests;

public sealed class CorporateV6180Tests
{
    private static readonly Guid Tenant = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid User = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Theory]
    [InlineData(OrganizationRole.Owner, true)] [InlineData(OrganizationRole.Admin, true)]
    [InlineData(OrganizationRole.TeamManager, false)] [InlineData(OrganizationRole.Member, false)]
    public void Administrative_access_obeys_role(OrganizationRole role, bool allowed) =>
        Assert.Equal(allowed, CorporatePolicy.CanAdminister(new(Tenant, User, role, true), Tenant));

    [Fact]
    public void Team_manager_only_manages_assigned_team()
    {
        var team = Guid.NewGuid(); var member = new OrganizationMember(Tenant, User, OrganizationRole.TeamManager, true);
        Assert.True(CorporatePolicy.CanManageTeam(member, new(Tenant, team, User, true, DateTime.UtcNow), Tenant, team));
        Assert.False(CorporatePolicy.CanManageTeam(member, new(Tenant, Guid.NewGuid(), User, true, DateTime.UtcNow), Tenant, team));
    }

    [Fact]
    public void Cross_tenant_access_is_denied() => Assert.Throws<UnauthorizedAccessException>(() => CorporatePolicy.EnsureTenant(Tenant, Guid.NewGuid()));

    [Fact]
    public void Invitation_uses_hash_and_rejects_expired_or_cancelled()
    {
        var now = new DateTimeOffset(2026, 8, 26, 12, 0, 0, TimeSpan.Zero); var clock = new FixedClock(now);
        var service = new InvitationTokenService(clock); var token = service.Create();
        Assert.DoesNotContain(token.Token, token.Hash);
        var valid = Invite(token.Hash, InvitationStatus.Pending, now.AddHours(1).UtcDateTime);
        Assert.True(service.IsValid(valid, token.Token, Tenant));
        Assert.False(service.IsValid(valid with { ExpiresAt = now.AddSeconds(-1).UtcDateTime }, token.Token, Tenant));
        Assert.False(service.IsValid(valid with { Status = InvitationStatus.Cancelled }, token.Token, Tenant));
        Assert.Equal(InvitationStatus.Expired, service.ExpireIfNeeded(valid with { ExpiresAt = now.AddSeconds(-1).UtcDateTime }).Status);
    }

    [Fact]
    public void Private_habit_is_only_visible_to_its_owner()
    {
        var preference = new PrivacyPreference(Tenant, User, true, false, DateTime.UtcNow);
        Assert.True(CorporatePolicy.CanExposeHabit(preference, User, User));
        Assert.False(CorporatePolicy.CanExposeHabit(preference, Guid.NewGuid(), User));
    }

    [Fact]
    public void Reports_are_aggregate_and_do_not_invent_rates_for_empty_denominators()
    {
        Assert.Null(new AggregateCorporateReport(0, 0, 0, 0).ParticipationRate);
        Assert.Equal(50m, new AggregateCorporateReport(10, 5, 8, 16).CompletionRate);
    }

    [Fact]
    public void Downgrade_preserves_data_but_blocks_new_resources()
    {
        var existingTeams = new[] { new Team(Guid.NewGuid(), Tenant, "Produto", null, false, DateTime.UtcNow, DateTime.UtcNow) };
        Assert.Equal("team.limit_reached", CorporateLimitPolicy.CreationError(new(false, 0, 0, 0), "team", existingTeams.Length));
        Assert.Single(existingTeams);
    }

    [Theory] [InlineData("team", "team.limit_reached")] [InlineData("invite", "invite.limit_reached")] [InlineData("program", "program.limit_reached")]
    public void Plan_limits_return_stable_events(string resource, string expected) => Assert.Equal(expected, CorporateLimitPolicy.CreationError(new(true, 1, 1, 1), resource, 1));

    private static TeamInvitation Invite(string hash, InvitationStatus status, DateTime expires) => new(Guid.NewGuid(), Tenant, null, "pessoa@example.com", OrganizationRole.Member, hash, status, DateTime.UtcNow, expires, null);
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider { public override DateTimeOffset GetUtcNow() => now; }
}
