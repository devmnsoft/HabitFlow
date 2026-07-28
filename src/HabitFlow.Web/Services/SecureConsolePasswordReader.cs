using System.Security;

namespace HabitFlow.Web.Services;

public sealed record PasswordPair(string Password, string Confirmation) : IDisposable
{
    public void Dispose() { /* Strings cannot be zeroed; their lifetime is deliberately kept to this command scope. */ }
}

public static class SecureConsolePasswordReader
{
    public static PasswordPair ReadAndConfirm()
    {
        if (Console.IsInputRedirected || Console.IsOutputRedirected)
            throw new InvalidOperationException("Este comando exige um terminal interativo para ler a senha com segurança.");
        Console.CancelKeyPress += OnCancel;
        try { return new(Read("Nova senha: "), Read("Confirme a nova senha: ")); }
        finally { Console.CancelKeyPress -= OnCancel; }
    }

    private static string Read(string prompt)
    {
        Console.Write(prompt);
        var chars = new List<char>(128);
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) { Console.WriteLine(); return new string(chars.ToArray()); }
            if (key.Key == ConsoleKey.Backspace) { if (chars.Count > 0) chars.RemoveAt(chars.Count - 1); continue; }
            if ((key.Modifiers & ConsoleModifiers.Control) != 0 && key.Key == ConsoleKey.C) throw new OperationCanceledException("Operação cancelada.");
            if (!char.IsControl(key.KeyChar) && chars.Count < 128) chars.Add(key.KeyChar);
        }
    }

    private static void OnCancel(object? sender, ConsoleCancelEventArgs e) { e.Cancel = true; throw new OperationCanceledException("Operação cancelada."); }
}
