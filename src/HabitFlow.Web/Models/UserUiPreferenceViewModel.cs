using HabitFlow.Domain;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HabitFlow.Web.Models;

public sealed class UserUiPreferenceViewModel
{
    public ContrastMode ContrastMode { get; set; } = ContrastMode.Default;
    public FontScale FontScale { get; set; } = FontScale.Normal;
    public bool ReduceMotion { get; set; }
    public bool ShowAchievementPopups { get; set; } = true;
    public bool ShowTipPopups { get; set; } = true;
    public bool EnableToasts { get; set; } = true;
    public bool ReducePopups { get; set; }

    public IReadOnlyList<SelectListItem> ContrastOptions => [new("Padrão", ContrastMode.Default.ToString()), new("Alto contraste", ContrastMode.HighContrast.ToString())];
    public IReadOnlyList<SelectListItem> FontOptions => [new("Normal", FontScale.Normal.ToString()), new("Maior", FontScale.Large.ToString())];
}
