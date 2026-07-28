using HabitFlow.Domain;

namespace HabitFlow.Web.Services;

/// <summary>Request-scoped plan snapshot; it naturally expires with the HTTP request.</summary>
public sealed class RequestPlanAccessContext
{
    public Guid? UserId { get; set; }
    public IReadOnlyDictionary<string, PlanFeatureValue>? Features { get; set; }
    public bool LoadFailed { get; set; }
}
