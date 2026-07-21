using System.Text.RegularExpressions;
using HabitFlow.Shared;

namespace HabitFlow.Application;

public sealed class WhatsAppValidator
{
    private static readonly Regex PhonePattern = new(@"^\+?[1-9]\d{10,14}$", RegexOptions.Compiled);
    private static readonly Regex HtmlPattern = new("<\\s*script|<[^>]+>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public Result Validate(WhatsAppOptions options)
    {
        if (!options.Enabled)
        {
            return Result.Success();
        }

        if (!PhonePattern.IsMatch(options.Number ?? string.Empty))
        {
            return Result.Failure("whatsapp.number", "Número inválido.");
        }

        if (HtmlPattern.IsMatch(options.DefaultMessage ?? string.Empty) || HtmlPattern.IsMatch(options.ButtonText ?? string.Empty))
        {
            return Result.Failure("whatsapp.html", "HTML não é permitido.");
        }

        return Result.Success();
    }
}
