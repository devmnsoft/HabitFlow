using System.ComponentModel.DataAnnotations;

namespace HabitFlow.Web.Models;

public sealed class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Informe seu e-mail.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [StringLength(254)]
    public string Email { get; set; } = string.Empty;
}

public sealed class ResetPasswordViewModel
{
    [Required] public string Token { get; set; } = string.Empty;
    [Required(ErrorMessage = "Informe a nova senha.")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "A senha deve ter entre 8 e 128 caracteres.")]
    [DataType(DataType.Password)] public string NewPassword { get; set; } = string.Empty;
    [Required(ErrorMessage = "Confirme a nova senha.")]
    [Compare(nameof(NewPassword), ErrorMessage = "As senhas não conferem.")]
    [DataType(DataType.Password)] public string ConfirmPassword { get; set; } = string.Empty;
}
