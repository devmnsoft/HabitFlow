using HabitFlow.Domain;

namespace HabitFlow.Application;

public sealed record PersonalRecommendationContext(string ObjectiveSlug, int AvailableMinutes, int[] AvailableDays,
    string PreferredFrequency, string PreferredPeriod, HabitDifficulty Difficulty, IReadOnlySet<Guid> ActiveTemplates,
    IReadOnlySet<Guid> FavoriteTemplates, int RemainingHabitSlots, string PlanCode);
public sealed record RecommendationExplanation(string Text);
public sealed record RecommendedHabitTemplate(HabitTemplate Template, RecommendationExplanation Explanation);
public sealed record RecommendedHabitCollection(HabitTemplateCollection Collection, RecommendationExplanation Explanation);
public sealed record RecommendationResult(IReadOnlyList<RecommendedHabitTemplate> Templates, IReadOnlyList<RecommendedHabitCollection> Collections);

public sealed class PersonalOnboardingRecommendationService
{
    public RecommendationResult Recommend(PersonalRecommendationContext context, IEnumerable<HabitTemplate> templates,
        IEnumerable<HabitTemplateCollection> collections)
    {
        var capacity = Math.Clamp(context.RemainingHabitSlots, 0, 6);
        var ranked = templates.Where(x => x.IsActive && !context.ActiveTemplates.Contains(x.Id) && Allowed(x.MinimumPlanCode, context.PlanCode))
            .Select(x => (Template:x, Score:Score(x,context)))
            .OrderByDescending(x=>x.Score).ThenBy(x=>x.Template.SortOrder).ThenBy(x=>x.Template.Id)
            .GroupBy(x=>x.Template.Category,StringComparer.OrdinalIgnoreCase).SelectMany(g=>g.Take(2))
            .Take(Math.Min(6,capacity)).Select(x=>new RecommendedHabitTemplate(x.Template,Explain(x.Template,context))).ToList();
        var recommendedCollections=collections.Where(x=>x.Status.Equals("published",StringComparison.OrdinalIgnoreCase)&&Allowed(x.MinimumPlanCode,context.PlanCode))
            .OrderByDescending(x=>x.ObjectiveId.HasValue).ThenByDescending(x=>x.IsFeatured).ThenBy(x=>x.SortOrder).ThenBy(x=>x.Id).Take(3)
            .Select(x=>new RecommendedHabitCollection(x,new($"Esta rotina reúne hábitos pequenos e cabe em cerca de {x.EstimatedTimeMinutes ?? context.AvailableMinutes} minutos."))).ToList();
        return new(ranked,recommendedCollections);
    }
    private static int Score(HabitTemplate t,PersonalRecommendationContext c)=>(t.EstimatedTimeMinutes is null||t.EstimatedTimeMinutes<=c.AvailableMinutes?30:0)+(t.Difficulty==c.Difficulty?20:0)+(c.FavoriteTemplates.Contains(t.Id)?15:0)+(t.IsFeatured?10:0);
    private static RecommendationExplanation Explain(HabitTemplate t,PersonalRecommendationContext c)
    {
        if(t.EstimatedTimeMinutes is int minutes && minutes<=c.AvailableMinutes)return new($"Cabe nos {c.AvailableMinutes} minutos que você reservou para o período da {Period(c.PreferredPeriod)}.");
        if(c.AvailableDays.Length>0)return new($"Você escolheu {c.AvailableDays.Length} dias por semana e esta sugestão pode começar nesse ritmo.");
        return new(t.WhyItHelps??"É uma forma simples de começar e ganhar consistência aos poucos.");
    }
    private static string Period(string value)=>value.ToLowerInvariant() switch{"morning" or "manha"=>"manhã","afternoon" or "tarde"=>"tarde",_=>"noite"};
    private static bool Allowed(string required,string actual)
    { var order=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase){{"free",0},{"premium",1},{"pro",2}};return order.GetValueOrDefault(actual,0)>=order.GetValueOrDefault(required,0); }
}

public sealed class PersonalOnboardingJourneyService(IUserOnboardingProgressRepository progress, IUserOnboardingDraftRepository drafts, TimeProvider clock)
{
    private readonly OnboardingJourneyService inner = new(progress,drafts,clock);
    public Task<UserOnboardingProgress> StartAsync(Guid clientId,Guid userId,CancellationToken ct=default)=>inner.StartAsync(clientId,userId,ct);
    public Task<UserOnboardingProgress?> ResumeAsync(Guid clientId,Guid userId,CancellationToken ct=default)=>inner.ResumeAsync(clientId,userId,ct);
    public Task<HabitFlow.Shared.Result<UserOnboardingProgress>> AdvanceAsync(UserOnboardingProgress next,int version,CancellationToken ct=default)=>inner.AdvanceAsync(next,version,ct);
    public Task<HabitFlow.Shared.Result<UserOnboardingProgress>> SkipAsync(Guid clientId,Guid userId,int version,CancellationToken ct=default)=>inner.SkipAsync(clientId,userId,version,ct);
}
