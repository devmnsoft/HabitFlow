namespace HabitFlow.Application;

public interface IUserFacingErrorMapper
{
    string ToPublicMessage(string? code, string? context = null);
    string ToAdminMessage(string? code, string? fallback = null);
}

public sealed class UserFacingErrorMapper : IUserFacingErrorMapper
{
    public string ToPublicMessage(string? code, string? context = null) => (code ?? string.Empty).ToLowerInvariant() switch
    {
        "postgres.invalid_password" => "Não foi possível concluir esta operação agora. Tente novamente em instantes.",
        "postgres.database_missing" => "Não foi possível acessar os dados necessários no momento.",
        "42p01" or "postgres.table_missing" => "Estamos finalizando uma configuração interna. Tente novamente em instantes.",
        var c when c.StartsWith("library.") && context == "habit-library" => "Não foi possível carregar todos os dados agora, mas você ainda pode explorar sugestões prontas.",
        _ => "Não foi possível carregar esta informação agora. Tente novamente em instantes."
    };

    public string ToAdminMessage(string? code, string? fallback = null) => (code ?? string.Empty).ToLowerInvariant() switch
    {
        "postgres.invalid_password" => "As credenciais do PostgreSQL estão incorretas.",
        "postgres.database_missing" => "O banco configurado não foi encontrado.",
        "42p01" or "postgres.table_missing" => "Uma tabela obrigatória do schema habitflow não foi encontrada.",
        _ => fallback ?? "Falha técnica registrada nos logs da aplicação."
    };
}
