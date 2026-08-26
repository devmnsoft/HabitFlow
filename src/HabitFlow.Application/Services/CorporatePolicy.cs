using System.Security.Cryptography;
using System.Text;
using HabitFlow.Domain;

namespace HabitFlow.Application;

public static class CorporatePolicy
{
    public static bool CanAdminister(OrganizationMember member, Guid clientId) => member.ClientId == clientId && member.IsActive && member.Role is OrganizationRole.Owner or OrganizationRole.Admin;
    public static bool CanReadAggregate(OrganizationMember member, Guid clientId) => member.ClientId == clientId && member.IsActive && member.Role is OrganizationRole.Owner or OrganizationRole.Admin or OrganizationRole.TeamManager or OrganizationRole.ReportReader;
    public static bool CanManageTeam(OrganizationMember member, TeamMember? membership, Guid clientId, Guid teamId) => CanAdminister(member, clientId) || (member.ClientId == clientId && member.IsActive && member.Role == OrganizationRole.TeamManager && membership is { IsManager: true } && membership.ClientId == clientId && membership.TeamId == teamId && membership.UserId == member.UserId);
    public static bool CanExposeHabit(PrivacyPreference preference, Guid requestingUserId, Guid habitOwnerId) => requestingUserId == habitOwnerId || (!preference.HabitsPrivate && preference.ShareProgramProgress);
    public static void EnsureTenant(Guid expectedClientId, Guid resourceClientId)
    {
        if (expectedClientId == Guid.Empty || expectedClientId != resourceClientId) throw new UnauthorizedAccessException("Recurso indisponível para esta organização.");
    }
}

public sealed class InvitationTokenService(TimeProvider clock)
{
    public (string Token, string Hash) Create()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return (token, Hash(token));
    }
    public bool IsValid(TeamInvitation invitation, string token, Guid clientId) => invitation.ClientId == clientId && invitation.Status == InvitationStatus.Pending && invitation.ExpiresAt > clock.GetUtcNow().UtcDateTime && CryptographicOperations.FixedTimeEquals(Convert.FromHexString(invitation.TokenHash), Convert.FromHexString(Hash(token)));
    public TeamInvitation ExpireIfNeeded(TeamInvitation invitation) => invitation.Status == InvitationStatus.Pending && invitation.ExpiresAt <= clock.GetUtcNow().UtcDateTime ? invitation with { Status = InvitationStatus.Expired, RespondedAt = clock.GetUtcNow().UtcDateTime } : invitation;
    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

public sealed record CorporatePlanLimits(bool CorporateEnabled, int Teams, int PendingInvites, int Programs);
public static class CorporateLimitPolicy
{
    public static string? CreationError(CorporatePlanLimits limits, string resource, int current) => resource switch
    {
        "team" when !limits.CorporateEnabled || current >= limits.Teams => "team.limit_reached",
        "invite" when !limits.CorporateEnabled || current >= limits.PendingInvites => "invite.limit_reached",
        "program" when !limits.CorporateEnabled || current >= limits.Programs => "program.limit_reached",
        _ => null
    };
}
