using System.ComponentModel.DataAnnotations;

namespace HabitFlow.Web.Models;

public sealed record MfaSettingsViewModel(bool IsEnabled, DateTime? EnabledAt, string? ManualKey = null, string? OtpAuthUri = null, IReadOnlyList<string>? RecoveryCodes = null);
public sealed class MfaCodeViewModel { [Required, RegularExpression("^[0-9]{6}$")] public string Code { get; set; } = string.Empty; }
public sealed class MfaChallengeViewModel { [Required] public Guid ChallengeId { get; set; } [Required] public string Code { get; set; } = string.Empty; }

