using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed class UserChallengeService(IUserChallengeRepository challenges, IHabitRepository habits,
    IUserRepository users, TimeProvider clock, ILogger<UserChallengeService> logger)
{
    private static readonly EventId StartedEvent = new(61651,"challenge_started");
    private static readonly EventId BlockedEvent = new(61652,"challenge_plan_blocked");
    private static readonly EventId AbandonedEvent = new(61653,"challenge_abandoned");

    public Task<IReadOnlyList<UserChallenge>> ListAsync(Guid clientId,Guid userId,CancellationToken ct=default) => challenges.ListAsync(clientId,userId,ct);

    public async Task<Result<UserChallenge>> StartAsync(Guid clientId,Guid userId,Guid habitId,int durationDays,string correlationId,CancellationToken ct=default)
    {
        if (durationDays is not (7 or 30 or 90)) return Result<UserChallenge>.Failure("challenge.duration","Escolha um desafio de 7, 30 ou 90 dias.");
        var user=await users.GetByIdAsync(userId,ct); var habit=await habits.GetAsync(clientId,userId,habitId,ct);
        if (user is null || user.ClientId!=clientId || user.AccountStatus!=AccountStatus.Active || habit is null)
            return Result<UserChallenge>.Failure("challenge.not_found","Hábito não encontrado.");
        if (habit.IsArchived || habit.IsPaused) return Result<UserChallenge>.Failure("challenge.habit_unavailable","Retome o hábito antes de iniciar o desafio.");
        if (durationDays>7 && user.Plan!=UserPlan.Premium)
        { logger.LogWarning(BlockedEvent,"Challenge blocked by plan. ClientId={ClientId} UserId={UserId} Days={Days} CorrelationId={CorrelationId}",clientId,userId,durationDays,correlationId); return Result<UserChallenge>.Failure("challenge.plan_required","Desafios de 30 e 90 dias fazem parte do Premium."); }
        if (await challenges.GetActiveAsync(clientId,userId,habitId,ct) is not null) return Result<UserChallenge>.Failure("challenge.duplicate","Este hábito já tem um desafio ativo.");
        var today=DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime); var now=clock.GetUtcNow().UtcDateTime;
        var challenge=new UserChallenge(Guid.NewGuid(),clientId,userId,habitId,$"{durationDays} dias de {habit.Name}","Um passo por dia, sem cobrança e com progresso visível.",durationDays,today,today.AddDays(durationDays-1),UserChallengeStatus.Active,now,now,null);
        await challenges.CreateAsync(challenge,ct);
        logger.LogInformation(StartedEvent,"Challenge started. ClientId={ClientId} UserId={UserId} ChallengeId={ChallengeId} Days={Days} CorrelationId={CorrelationId}",clientId,userId,challenge.Id,durationDays,correlationId);
        return Result<UserChallenge>.Success(challenge);
    }

    public async Task<Result> AbandonAsync(Guid clientId,Guid userId,Guid challengeId,string correlationId,CancellationToken ct=default)
    {
        if (!await challenges.SetStatusAsync(clientId,userId,challengeId,UserChallengeStatus.Abandoned,clock.GetUtcNow().UtcDateTime,ct)) return Result.Failure("challenge.not_found","Desafio ativo não encontrado.");
        logger.LogInformation(AbandonedEvent,"Challenge abandoned. ClientId={ClientId} UserId={UserId} ChallengeId={ChallengeId} CorrelationId={CorrelationId}",clientId,userId,challengeId,correlationId); return Result.Success();
    }
}
