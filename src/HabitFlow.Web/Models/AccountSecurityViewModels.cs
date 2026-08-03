using System.ComponentModel.DataAnnotations;

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
