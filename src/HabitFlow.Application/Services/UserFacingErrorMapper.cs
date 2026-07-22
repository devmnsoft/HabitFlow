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
        "postgres.invalid_password" => "Não conseguimos acessar os dados necessários agora.",
        "postgres.database_missing" => "Não conseguimos localizar os dados necessários agora.",
        "42p01" or "postgres.table_missing" => "O sistema ainda não encontrou todas as informações necessárias.",
        "dapper.datetime_unsupported" or "notsupportedexception" => "Não foi possível carregar esta informação agora. Tente novamente em instantes.",
        var c when c.StartsWith("library.") && context == "habit-library" => "Não foi possível carregar todos os dados agora, mas você ainda pode explorar sugestões prontas.",
        _ => "Não foi possível carregar esta informação agora. Tente novamente em instantes."
    };

    public string ToAdminMessage(string? code, string? fallback = null) => (code ?? string.Empty).ToLowerInvariant() switch
    {
        "postgres.invalid_password" => "Senha do PostgreSQL incorreta para o usuário configurado.",
        "postgres.database_missing" => "Banco habitflow não existe ou connection string aponta para banco incorreto.",
        "42p01" or "postgres.table_missing" => "Execute database/script_completo.sql.",
        "dapper.datetime_unsupported" or "notsupportedexception" => "Dapper não reconheceu DateOnly/TimeOnly. Verifique DapperTypeHandlers.Register().",
        _ => fallback ?? "Falha técnica registrada nos logs da aplicação."
    };
}
