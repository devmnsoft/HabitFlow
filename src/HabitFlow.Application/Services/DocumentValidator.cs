using System.Text.RegularExpressions;

namespace HabitFlow.Application;

public sealed class DocumentValidator
{
    public string Normalize(string? document) => string.IsNullOrWhiteSpace(document) ? string.Empty : Regex.Replace(document, "\\D", string.Empty);
    public bool IsCpf(string? document) => Normalize(document).Length == 11;
    public bool IsCnpj(string? document) => Normalize(document).Length == 14;
    public bool ValidateCpf(string? cpf)
    {
        var n = Normalize(cpf);
        if (n.Length != 11 || n.Distinct().Count() == 1) return false;
        var sum = 0; for (var i = 0; i < 9; i++) sum += (n[i] - '0') * (10 - i);
        var d1 = sum % 11 < 2 ? 0 : 11 - (sum % 11); if (d1 != n[9] - '0') return false;
        sum = 0; for (var i = 0; i < 10; i++) sum += (n[i] - '0') * (11 - i);
        var d2 = sum % 11 < 2 ? 0 : 11 - (sum % 11); return d2 == n[10] - '0';
    }
    public bool ValidateCnpj(string? cnpj)
    {
        var n = Normalize(cnpj);
        if (n.Length != 14 || n.Distinct().Count() == 1) return false;
        int[] w1 = [5,4,3,2,9,8,7,6,5,4,3,2]; int[] w2 = [6,5,4,3,2,9,8,7,6,5,4,3,2];
        var sum = 0; for (var i = 0; i < 12; i++) sum += (n[i] - '0') * w1[i];
        var d1 = sum % 11 < 2 ? 0 : 11 - (sum % 11); if (d1 != n[12] - '0') return false;
        sum = 0; for (var i = 0; i < 13; i++) sum += (n[i] - '0') * w2[i];
        var d2 = sum % 11 < 2 ? 0 : 11 - (sum % 11); return d2 == n[13] - '0';
    }
    public string FormatCpf(string? cpf) { var n = Normalize(cpf); return n.Length == 11 ? $"{n[..3]}.{n[3..6]}.{n[6..9]}-{n[9..]}" : n; }
    public string FormatCnpj(string? cnpj) { var n = Normalize(cnpj); return n.Length == 14 ? $"{n[..2]}.{n[2..5]}.{n[5..8]}/{n[8..12]}-{n[12..]}" : n; }
    public string GetDocumentTypeByPersonType(string? personType) => string.Equals(personType, "NaturalPerson", StringComparison.OrdinalIgnoreCase) ? "CPF" : string.Equals(personType, "LegalPerson", StringComparison.OrdinalIgnoreCase) ? "CNPJ" : string.Empty;
}
