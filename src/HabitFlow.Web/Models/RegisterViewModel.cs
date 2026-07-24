using System.ComponentModel.DataAnnotations;
using HabitFlow.Application;

namespace HabitFlow.Web.Models;

public sealed class RegisterViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Escolha Pessoa Física ou Pessoa Jurídica.")]
    public string ClientPersonType { get; set; } = "NaturalPerson";
    public string DocumentType { get; set; } = "CPF";
    [Required(ErrorMessage = "Informe o CPF/CNPJ.")]
    public string Document { get; set; } = string.Empty;
    [StringLength(120, ErrorMessage = "Use no máximo 120 caracteres.")]
    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? TradeName { get; set; }
    public string? ResponsibleName { get; set; }
    [Required(ErrorMessage = "Informe seu e-mail.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    [Required(ErrorMessage = "Informe sua senha.")]
    [MinLength(8, ErrorMessage = "A senha deve ter pelo menos 8 caracteres.")]
    public string Password { get; set; } = string.Empty;
    [Required(ErrorMessage = "Confirme sua senha.")]
    [Compare(nameof(Password), ErrorMessage = "As senhas não conferem.")]
    public string ConfirmPassword { get; set; } = string.Empty;
    public bool AcceptedTerms { get; set; }
    public bool AcceptedPrivacy { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ClientPersonType == "NaturalPerson" && string.IsNullOrWhiteSpace(Name)) yield return new("Informe seu nome completo.", [nameof(Name)]);
        if (ClientPersonType == "LegalPerson" && string.IsNullOrWhiteSpace(LegalName)) yield return new("Informe a razão social.", [nameof(LegalName)]);
        if (ClientPersonType == "LegalPerson" && string.IsNullOrWhiteSpace(ResponsibleName)) yield return new("Informe o nome do responsável.", [nameof(ResponsibleName)]);
        if (!AcceptedTerms) yield return new("Aceite os Termos de Uso para continuar.", [nameof(AcceptedTerms)]);
        if (!AcceptedPrivacy) yield return new("Aceite a Política de Privacidade para continuar.", [nameof(AcceptedPrivacy)]);
    }

    public RegisterDto ToDto() => new(Name, Email, Password, ConfirmPassword);
    public RegisterClientAccountDto ToClientAccountDto()
    {
        var type = ClientPersonType == "LegalPerson" ? "LegalPerson" : "NaturalPerson";
        return new(type, type == "NaturalPerson" ? "CPF" : "CNPJ", Document, string.Empty, type == "NaturalPerson" ? Name : (LegalName ?? string.Empty), LegalName, TradeName, ResponsibleName, Email, Phone, Password, AcceptedTerms, AcceptedPrivacy);
    }
}
