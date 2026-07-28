# Segurança da redefinição

Tokens contêm 32 bytes de `RandomNumberGenerator`, usam Base64Url, expiram por padrão em 30 minutos e são armazenados somente como SHA-256. São vinculados ao usuário, revogáveis e de uso único. IP e User-Agent são armazenados apenas como hashes. As páginas aplicam antiforgery, `no-store`, `no-cache` e `no-referrer`; tokens, senhas e Authorization nunca devem ser registrados.

`Email:PasswordReset:PublicBaseUrl` é configuração confiável, deve pertencer a `AllowedBaseUrls`, usar HTTPS e jamais ser localhost em produção. Nunca derive links de `Host` ou headers encaminhados.
