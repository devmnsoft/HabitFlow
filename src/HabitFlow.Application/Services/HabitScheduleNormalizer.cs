using HabitFlow.Domain;
using HabitFlow.Shared;

namespace HabitFlow.Application;

public sealed record HabitScheduleInput(HabitFrequencyType FrequencyType, int? TargetPerWeek, IReadOnlyCollection<int>? SelectedDays);
public sealed record HabitScheduleNormalizedInput(HabitFrequencyType FrequencyType, int TargetPerWeek, IReadOnlyList<int> SelectedDays);

public sealed class HabitScheduleNormalizer
{
    public Result<HabitScheduleNormalizedInput> Normalize(HabitScheduleInput input)
    {
        if (!Enum.IsDefined(input.FrequencyType))
            return Result<HabitScheduleNormalizedInput>.Failure("habit.frequency_invalid", "Escolha uma frequência válida.");

        var submittedDays = input.SelectedDays?.Distinct().OrderBy(x => x).ToArray() ?? [];
        var days = input.FrequencyType == HabitFrequencyType.CustomWeekly ? submittedDays : [];
        if (days.Any(x => x is < 0 or > 6))
            return Result<HabitScheduleNormalizedInput>.Failure("habit.weekday_invalid", "Selecione dias da semana válidos.");
        if (input.FrequencyType == HabitFrequencyType.CustomWeekly && days.Length == 0)
            return Result<HabitScheduleNormalizedInput>.Failure("habit.custom_days_required", "Selecione pelo menos um dia da semana.");

        var maximum = input.FrequencyType switch
        {
            HabitFrequencyType.Daily => 7,
            HabitFrequencyType.Weekdays => 5,
            HabitFrequencyType.Weekends => 2,
            HabitFrequencyType.CustomWeekly => days.Length,
            _ => 0
        };
        var target = input.TargetPerWeek ?? maximum;
        if (target < 1 || target > maximum)
            return Result<HabitScheduleNormalizedInput>.Failure("habit.target_invalid", "A meta semanal não combina com a frequência escolhida.");

        return Result<HabitScheduleNormalizedInput>.Success(new(input.FrequencyType, target, days));
    }
}
