using System.Text.Json;
using System.Text.RegularExpressions;

namespace HabitFlow.Application;

public sealed class LogSanitizer
{
    private static readonly Regex SecretPattern = new(
        "(?i)(password|senha|token|cookie|secret|authorization)\\s*[:=]\\s*([^,;&\\s]+)",
        RegexOptions.Compiled);

    public string Sanitize(string? input) =>
        string.IsNullOrWhiteSpace(input) ? string.Empty : SecretPattern.Replace(input, "$1=***");

    public string SanitizeJson(object metadata) => Sanitize(JsonSerializer.Serialize(metadata));
}
