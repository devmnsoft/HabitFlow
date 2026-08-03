using System.ComponentModel.DataAnnotations;
using HabitFlow.Domain;

namespace HabitFlow.Web.Models;

public sealed class HabitReminderEditorViewModel
{
    public Guid HabitId { get; set; }
    public string HabitName { get; set; } = "";
    [Required, DataType(DataType.Time)] public TimeOnly ReminderTime { get; set; } = new(8, 0);
    [Required] public int[] Days { get; set; } = [1, 2, 3, 4, 5];
    [Required, StringLength(80)] public string Timezone { get; set; } = "America/Sao_Paulo";
    public IReadOnlyList<HabitReminder> Existing { get; set; } = [];
}

public sealed record ReminderListViewModel(IReadOnlyList<HabitReminder> Reminders);
