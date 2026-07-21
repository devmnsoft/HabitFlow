namespace HabitFlow.Application;

public enum FeedbackType { Toast, Modal, Inline, SilentLog }
public enum FeedbackSeverity { Success, Info, Warning, Error, Database }
public sealed record FeedbackDescriptor(FeedbackType FeedbackType, FeedbackSeverity Severity, string Title, string UserMessage, string? DeveloperMessage = null, bool IsTechnical = false);

public sealed class FeedbackMapper
{
    public FeedbackDescriptor Map(string? code, string? fallback = null, bool isAdminOrDevelopment = false)
    {
        var normalized = (code ?? string.Empty).ToLowerInvariant();
        return normalized switch
        {
            "postgres.invalid_password" => Database("Não conseguimos acessar os dados necessários agora.", "Senha do PostgreSQL incorreta para o usuário configurado."),
            "postgres.database_missing" => Database("Não conseguimos localizar os dados necessários agora.", "Banco habitflow não existe ou connection string aponta para banco incorreto."),
            "42p01" or "postgres.table_missing" => Database("O sistema ainda não encontrou todas as informações necessárias.", "Execute database/script_completo.sql."),
            var c when c.StartsWith("validation.") => new(FeedbackType.Inline, FeedbackSeverity.Warning, "Revise os dados", fallback ?? "Confira os campos destacados."),
            var c when c.StartsWith("auth.invalid") => new(FeedbackType.Toast, FeedbackSeverity.Warning, "Login não realizado", "E-mail ou senha inválidos."),
            _ => new(FeedbackType.Toast, FeedbackSeverity.Warning, "Não foi possível concluir", fallback ?? "Tente novamente em instantes.")
        };
        FeedbackDescriptor Database(string user, string dev) => new(FeedbackType.Modal, FeedbackSeverity.Database, isAdminOrDevelopment ? "Falha na conexão com PostgreSQL" : "Não conseguimos concluir agora", isAdminOrDevelopment ? "Verifique Host, Database, Username e Password em appsettings.Development.local.json." : user, dev, true);
    }
}
