using HabitFlow.Application;

namespace HabitFlow.Web.Services;

public static class AdminCli
{
    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase) { "--email", "--name", "--connection-string-name" };
    private static readonly HashSet<string> Forbidden = new(StringComparer.OrdinalIgnoreCase) { "--password", "--password-hash", "--token", "--secret" };

    public static bool IsCommand(string[] args) => args.Length >= 2 && args[0].Equals("admin", StringComparison.OrdinalIgnoreCase);

    public static async Task<int> RunAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        try
        {
            var options = Parse(args.Skip(2).ToArray());
            if (options.TryGetValue("--connection-string-name", out var connectionName) && !connectionName.Equals("DefaultConnection", StringComparison.Ordinal))
                throw new InvalidOperationException("Somente a configuração DefaultConnection pode ser usada.");
            var email = Get(options, "--email", "E-mail: ");
            var actor = $"local-cli:{Environment.UserName}";
            var correlationId = Guid.NewGuid().ToString("N");
            await using var scope = services.CreateAsyncScope();
            SuperAdminProvisioningResult result;
            switch (args[1].ToLowerInvariant())
            {
                case "create-superadmin":
                    var name = Get(options, "--name", "Nome: ");
                    using (var passwords = SecureConsolePasswordReader.ReadAndConfirm())
                        result = await scope.ServiceProvider.GetRequiredService<CreateSuperAdminHandler>().HandleAsync(new(name, email, passwords.Password, passwords.Confirmation, actor, "provisionamento administrativo local", correlationId), ct);
                    break;
                case "reset-superadmin-password":
                    using (var passwords = SecureConsolePasswordReader.ReadAndConfirm())
                        result = await scope.ServiceProvider.GetRequiredService<ResetSuperAdminPasswordHandler>().HandleAsync(new(email, passwords.Password, passwords.Confirmation, actor, "redefinição administrativa local", correlationId), ct);
                    break;
                case "promote-superadmin":
                    Console.Write("Digite PROMOVER para confirmar: ");
                    if (!string.Equals(Console.ReadLine(), "PROMOVER", StringComparison.Ordinal)) return Fail("Promoção cancelada.");
                    result = await scope.ServiceProvider.GetRequiredService<PromoteSuperAdminHandler>().HandleAsync(new(email, actor, "promoção administrativa confirmada", correlationId), ct);
                    break;
                default: return Fail("Comando administrativo desconhecido.");
            }
            Console.WriteLine(result.Message);
            return result.Success ? 0 : 2;
        }
        catch (OperationCanceledException) { return Fail("Operação cancelada."); }
        catch (InvalidOperationException ex) { return Fail(ex.Message); }
        catch (Exception) { return Fail("Não foi possível acessar o banco. Verifique credenciais, schema e migrations no log local seguro."); }
    }

    private static Dictionary<string, string> Parse(string[] args)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i += 2)
        {
            var key = args[i];
            if (Forbidden.Contains(key)) throw new InvalidOperationException($"O argumento {key} é proibido. Senhas e segredos nunca podem ser informados na linha de comando.");
            if (!Allowed.Contains(key) || i + 1 >= args.Length) throw new InvalidOperationException($"Argumento inválido: {key}.");
            result[key] = args[i + 1];
        }
        return result;
    }
    private static string Get(IReadOnlyDictionary<string, string> values, string key, string prompt) { if (values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)) return value; Console.Write(prompt); return Console.ReadLine()?.Trim() ?? ""; }
    private static int Fail(string message) { Console.Error.WriteLine(message); return 2; }
}
