using HabitFlow.Application;

namespace HabitFlow.Web.Services;

public static class AdminCli
{
    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase) { "--email", "--name", "--connection-string-name", "--generate-password", "--prompt-password", "--reset-existing" };
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
                case "create-dev-superadmin":
                    var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
                    if (!environment.IsDevelopment()) return Fail("Este comando funciona exclusivamente em Development.");
                    if (Console.IsInputRedirected || Console.IsOutputRedirected || Console.IsErrorRedirected)
                        return Fail("Este comando exige um terminal interativo sem redirecionamento.");
                    var devName = Get(options, "--name", "Nome: ", scope.ServiceProvider.GetRequiredService<IConfiguration>()["HabitFlowDev:SuperAdmin:Name"] ?? "Administrador HabitFlow");
                    email = Get(options, "--email", "E-mail: ", scope.ServiceProvider.GetRequiredService<IConfiguration>()["HabitFlowDev:SuperAdmin:Email"] ?? "superadmin@habitflow.local");
                    var generate = options.ContainsKey("--generate-password");
                    string temporaryPassword;
                    if (generate) temporaryPassword = SecureDevelopmentPasswordGenerator.Generate();
                    else using (var passwords = SecureConsolePasswordReader.ReadAndConfirm()) temporaryPassword = passwords.Password;
                    result = await scope.ServiceProvider.GetRequiredService<CreateDevelopmentSuperAdminHandler>().HandleAsync(
                        new(devName, email, temporaryPassword, actor, correlationId), ct);
                    if (!result.Success) return Fail(result.Message);
                    Console.WriteLine($"Login: {email}");
                    Console.WriteLine($"Senha temporária: {temporaryPassword}");
                    Console.WriteLine("Altere esta senha após o primeiro acesso.");
                    return 0;
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
        for (var i = 0; i < args.Length;)
        {
            var key = args[i];
            if (Forbidden.Contains(key)) throw new InvalidOperationException($"O argumento {key} é proibido. Senhas e segredos nunca podem ser informados na linha de comando.");
            if (!Allowed.Contains(key)) throw new InvalidOperationException($"Argumento inválido: {key}.");
            if (key is "--generate-password" or "--prompt-password" or "--reset-existing") { result[key] = "true"; i++; continue; }
            if (i + 1 >= args.Length) throw new InvalidOperationException($"Valor ausente para {key}.");
            result[key] = args[i + 1];
            i += 2;
        }
        return result;
    }
    private static string Get(IReadOnlyDictionary<string, string> values, string key, string prompt, string? defaultValue = null) { if (values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)) return value; Console.Write(defaultValue is null ? prompt : $"{prompt.Trim()} [{defaultValue}]: "); var entered = Console.ReadLine()?.Trim(); return string.IsNullOrWhiteSpace(entered) ? defaultValue ?? "" : entered; }
    private static int Fail(string message) { Console.Error.WriteLine(message); return 2; }
}

public static class SecureDevelopmentPasswordGenerator
{
    private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lower = "abcdefghijkmnopqrstuvwxyz";
    private const string Digits = "23456789";
    private const string Special = "!@#$%&*+-_=?";

    public static string Generate(int length = 28)
    {
        if (length < 24) throw new ArgumentOutOfRangeException(nameof(length));
        var chars = new List<char> { Pick(Upper), Pick(Lower), Pick(Digits), Pick(Special) };
        var all = Upper + Lower + Digits + Special;
        while (chars.Count < length) chars.Add(Pick(all));
        for (var i = chars.Count - 1; i > 0; i--)
        {
            var j = System.Security.Cryptography.RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
        return new string(chars.ToArray());
    }
    private static char Pick(string source) => source[System.Security.Cryptography.RandomNumberGenerator.GetInt32(source.Length)];
}
