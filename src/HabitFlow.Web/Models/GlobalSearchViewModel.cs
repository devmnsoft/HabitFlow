namespace HabitFlow.Web.Models;

public sealed record GlobalSearchItemViewModel(string Type, string Title, string Description, string Url, string Icon);
public sealed record GlobalSearchViewModel(string Query, IReadOnlyList<GlobalSearchItemViewModel> Groups);
