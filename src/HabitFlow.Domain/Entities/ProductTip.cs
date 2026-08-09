namespace HabitFlow.Domain;

public sealed record ProductTip(Guid Id, string Code, string RoutePattern, string TargetSelector,
    string Title, string Content, int DisplayOrder, bool IsActive);

public sealed record UserProductTip(Guid UserId, Guid ProductTipId, DateTime? SeenAt,
    DateTime? DismissedAt, DateTime UpdatedAt);

