using HabitFlow.Domain;

namespace HabitFlow.Web.Models;

public sealed record ChallengePageViewModel(IReadOnlyList<UserChallenge> Challenges, IReadOnlyList<Habit> Habits);
