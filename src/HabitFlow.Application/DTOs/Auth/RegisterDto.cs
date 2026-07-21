using System.ComponentModel.DataAnnotations;

namespace HabitFlow.Application;

public sealed record RegisterDto(
    [Required(ErrorMessage = "Informe seu nome.")]
    string Name,
    [Required(ErrorMessage = "Informe seu e-mail.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    string Email,
    [Required(ErrorMessage = "Informe uma senha para proteger sua conta.")]
    [MinLength(8, ErrorMessage = "A senha deve ter pelo menos 8 caracteres.")]
    string Password,
    [Required(ErrorMessage = "Confirme sua senha para continuar.")]
    [Compare(nameof(Password), ErrorMessage = "As senhas não conferem. Digite a mesma senha nos dois campos.")]
    string ConfirmPassword);
