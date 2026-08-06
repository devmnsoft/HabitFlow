using System.ComponentModel.DataAnnotations;
using HabitFlow.Application;

namespace HabitFlow.Web.Models;

public sealed class RequiredPasswordChangeViewModel
{
    [Required, DataType(DataType.Password), Display(Name = "Senha atual")]
    public string CurrentPassword { get; set; } = string.Empty;
    [Required, DataType(DataType.Password), MinLength(8), MaxLength(128), Display(Name = "Nova senha")]
    public string NewPassword { get; set; } = string.Empty;
    [Required, DataType(DataType.Password), Compare(nameof(NewPassword)), Display(Name = "Confirme a nova senha")]
    public string Confirmation { get; set; } = string.Empty;
}

public sealed record AccountSecurityViewModel(string Email, DateTime? LastPasswordChange, IReadOnlyList<AccountSession> Sessions, Guid? CurrentSessionId);

public sealed class RevokeSessionViewModel
{
    [Required] public Guid SessionId { get; set; }
    [Required, DataType(DataType.Password), Display(Name = "Senha atual")] public string Password { get; set; } = string.Empty;
}

public sealed class RevokeAllSessionsViewModel
{
    [Required, DataType(DataType.Password), Display(Name = "Senha atual")] public string Password { get; set; } = string.Empty;
}
