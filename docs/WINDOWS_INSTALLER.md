# WINDOWS_INSTALLER

HabitFlow v4.4-WindowsIIS-Production-NoDocker formaliza operação em Windows/IIS sem tornar Docker obrigatório.

## Fluxo principal
1. Instale .NET Hosting Bundle, IIS e PostgreSQL 16.
2. Rode `powershell -ExecutionPolicy Bypass -File scripts/windows/check-environment.ps1`.
3. Crie o banco com `scripts/windows/setup-postgres-database.ps1 -DatabaseName habitflow`.
4. Aplique `database/script_completo.sql` com `scripts/windows/apply-database-script.ps1 -DatabaseName habitflow`.
5. Gere configuração local com `scripts/windows/generate-production-config.ps1` e nunca versione secrets.
6. Publique no IIS com `scripts/windows/publish-iis.ps1 -Confirm PUBLICAR_HABITFLOW_IIS`.
7. Valide com `scripts/windows/smoke-test.ps1 -BaseUrl http://localhost:5097` ou URL HTTPS pública.

## Erros comuns
- 500.30: confirme Hosting Bundle, Event Viewer e habilite stdout temporariamente no web.config apenas para diagnóstico.
- Connection string inválida: valide Host, Port, Database, Username e Password.
- psql não encontrado: adicione `C:\Program Files\PostgreSQL\16\bin` ao PATH.
- Permissão IIS: conceda leitura ao AppPool e escrita somente em pastas necessárias.
- App pool errado: use No Managed Code para ASP.NET Core hospedado pelo ANCM.
- Banco não criado: execute setup-postgres-database antes do apply.
- SSL/cookie secure: habilite HTTPS antes de CookieSecure em produção.
