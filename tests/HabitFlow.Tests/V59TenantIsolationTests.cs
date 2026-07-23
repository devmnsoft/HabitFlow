using HabitFlow.Application;
using HabitFlow.Domain;

namespace HabitFlow.Tests;

public sealed class V59TenantIsolationTests
{
    [Fact]
    public void InviteTokenHash_DoesNotStorePlainToken()
    {
        var token = "plain-token";
        var hash = UserInviteService.HashToken(token);
        Assert.NotEqual(token, hash);
        Assert.Equal(64, hash.Length);
    }

    [Fact]
    public void UserInvite_IsPending_RequiresPendingStatusAndFutureExpiration()
    {
        var invite = new UserInvite(Guid.NewGuid(), Guid.NewGuid(), "user@example.com", UserRole.User, "hash", UserInviteStatus.Pending, null, null, DateTime.UtcNow.AddMinutes(5), null, null, DateTime.UtcNow, DateTime.UtcNow);
        Assert.True(invite.IsPending(DateTime.UtcNow));
    }

    [Fact]
    public void UserInvite_ExpiredInviteIsNotPending()
    {
        var invite = new UserInvite(Guid.NewGuid(), Guid.NewGuid(), "user@example.com", UserRole.User, "hash", UserInviteStatus.Pending, null, null, DateTime.UtcNow.AddMinutes(-5), null, null, DateTime.UtcNow, DateTime.UtcNow);
        Assert.False(invite.IsPending(DateTime.UtcNow));
    }
}
