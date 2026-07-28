# Reset da senha do SuperAdmin

Execute `dotnet run --project src/HabitFlow.Web -- admin reset-superadmin-password --email admin@example.com` em terminal interativo. A política oficial (8–128 caracteres, sem espaços nas bordas, diferente de nome/e-mail e da senha atual) é reutilizada.

Na mesma transação são atualizados o hash BCrypt, `session_version`, tokens de recuperação ativos e auditorias `superadmin.password_reset`/`superadmin.session_revoked`. Não ocorre login automático nem envio de senha por e-mail.
