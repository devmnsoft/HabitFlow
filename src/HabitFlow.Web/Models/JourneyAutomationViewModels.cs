using HabitFlow.Domain;

namespace HabitFlow.Web.Models;

public sealed record JourneysIndexViewModel(IReadOnlyList<HabitJourney> Journeys);
public sealed record JourneyDetailsViewModel(HabitJourneyDetails Details);
public sealed record AutomationsIndexViewModel(IReadOnlyList<HabitAutomation> Automations);
