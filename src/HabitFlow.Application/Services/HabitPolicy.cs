using HabitFlow.Domain;
using HabitFlow.Shared;

namespace HabitFlow.Application;

public sealed class HabitPolicy
{
    public Result CanCreate(User user, int active) =>
        DomainPolicies.CanCreateHabit(user, active)
            ? Result.Success()
            : Result.Failure("plan.free_limit", "Plano gratuito limitado a 5 hábitos ativos.");
}
