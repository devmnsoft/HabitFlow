# TELEGRAM

HabitFlow v4 usa ASP.NET Core 10, DDD, Clean Architecture, Dapper, PostgreSQL, Bootstrap 5 e JavaScript Vanilla.

- Código principal: src/.
- Banco: database/migrations e database/seeds.
- Docker: porta 5097 para a aplicação e PostgreSQL 16.
- IIS: publicar em publish/windows com web.config e App Pool No Managed Code.
- Segurança: sem secrets no Git, sem stack trace em produção, cookies seguros, BCrypt e SQL parametrizado.
- LGPD: exportação e exclusão são registradas em habitflow.lgpd_requests.
- Legado Firebase: preservado como referência em legacy-firebase/ quando aplicável e não usado como backend principal.

## v4.3 Ações críticas

Ações críticas administrativas devem usar o serviço de Telegram quando habilitado: bloqueio, suspensão, marcação suspeita, alteração manual de plano, exportação CSV e alterações LGPD críticas. Mensagens não devem conter dados sensíveis.
