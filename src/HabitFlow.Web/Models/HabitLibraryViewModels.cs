using HabitFlow.Domain;

namespace HabitFlow.Web.Models;

public sealed record HabitTemplateDetailsViewModel(HabitTemplate Template, bool IsFavorite);
