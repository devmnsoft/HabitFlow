using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class UserUiPreferenceRepository(SqlExecutor db) : IUserUiPreferenceRepository
{
    private const string Columns = "id, user_id, contrast_mode, font_scale, reduce_motion, show_achievement_popups, show_tip_popups, enable_toasts, reduce_popups, created_at, updated_at";

    public Task<UserUiPreference?> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        db.QuerySingleOrDefaultAsync<UserUiPreference>("select " + Columns + " from habitflow.user_ui_preferences where user_id = @userId", new { userId }, ct);

    public Task UpsertAsync(UserUiPreference p, CancellationToken ct = default) =>
        db.ExecuteAsync(@"
insert into habitflow.user_ui_preferences(id, user_id, contrast_mode, font_scale, reduce_motion, show_achievement_popups, show_tip_popups, enable_toasts, reduce_popups, created_at, updated_at)
values(@Id, @UserId, @ContrastMode::text, @FontScale::text, @ReduceMotion, @ShowAchievementPopups, @ShowTipPopups, @EnableToasts, @ReducePopups, @CreatedAt, @UpdatedAt)
on conflict(user_id) do update set
  contrast_mode = excluded.contrast_mode,
  font_scale = excluded.font_scale,
  reduce_motion = excluded.reduce_motion,
  show_achievement_popups = excluded.show_achievement_popups,
  show_tip_popups = excluded.show_tip_popups,
  enable_toasts = excluded.enable_toasts,
  reduce_popups = excluded.reduce_popups,
  updated_at = now();", p, ct);
}
