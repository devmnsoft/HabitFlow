using System.ComponentModel.DataAnnotations;
using HabitFlow.Application;

namespace HabitFlow.Web.Models;

public sealed class RegisterViewModel
{
    [Required(ErrorMessage = "Informe seu nome.")]
    [StringLength(120, ErrorMessage = "Use no máximo 120 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe seu e-mail.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe sua senha.")]
    [MinLength(8, ErrorMessage = "A senha deve ter pelo menos 8 caracteres.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme sua senha.")]
    [Compare(nameof(Password), ErrorMessage = "As senhas não conferem.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public RegisterDto ToDto() => new(Name, Email, Password, ConfirmPassword);
}
