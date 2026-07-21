namespace HabitFlow.Domain;

public static class DomainPolicies
{
    public const int FreePlanActiveHabitLimit = 5;

    public static bool CanCreateHabit(User user, int activeHabits) =>
        user.Plan == UserPlan.Premium || activeHabits < FreePlanActiveHabitLimit;

    public static bool CanChangeHabit(User user, Habit habit) =>
        user.Role == UserRole.Admin || habit.UserId == user.Id;
}
