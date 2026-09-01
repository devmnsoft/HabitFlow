using System.Text.Json;
using System.Text.RegularExpressions;

namespace HabitFlow.Application;

public sealed class LogSanitizer
{
    private static readonly Regex SecretPattern = new(
        "(?i)(password|senha|token|cookie|secret|authorization|connectionstring)\\s*[:=]\\s*([^,;&\\s]+)",
        RegexOptions.Compiled);
    private static readonly Regex DocumentPattern = new(@"(?<!\d)(\d{3})\.?\d{3}\.?\d{3}-?\d{2}(?!\d)|(?<!\d)(\d{2})\.?\d{3}\.?\d{3}/?\d{4}-?\d{2}(?!\d)", RegexOptions.Compiled);

    public string Sanitize(string? input) =>
        string.IsNullOrWhiteSpace(input) ? string.Empty : DocumentPattern.Replace(SecretPattern.Replace(input, "$1=***"), m => m.Value.Length > 14 ? m.Value[..3] + ".***.***/****-**" : m.Value[..3] + ".***.***-**");

    public string SanitizeJson(object metadata) => Sanitize(JsonSerializer.Serialize(metadata));
}
