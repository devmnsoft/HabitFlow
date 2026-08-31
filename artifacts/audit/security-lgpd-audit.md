# Auditoria de segurança e LGPD — HabitFlow v6.19.0

Data: 2026-08-31. Escopo: arquivos texto versionados em `src/` e `tests/`; binários e anexos foram ignorados.

## Método

Foram pesquisados controllers, repositories Dapper, middleware, configuração de autenticação, views e testes por autorização, escopo de tenant/usuário, SQL interpolado, segredos, dados sensíveis em logs, antiforgery, redirects, upload, `CancellationToken` e rotas administrativas.

## Achados corrigidos

1. **ALTO — consultas LGPD sem vínculo explícito ao tenant.** A leitura de solicitações e consentimentos filtrava apenas `user_id`. Agora exige `client_id` e confirma o vínculo na tabela de usuários.
2. **ALTO — portabilidade era apenas uma solicitação, sem arquivo útil.** Foi criada exportação JSON síncrona e autenticada, com função SQL tenant-safe. A seleção exclui hash de senha, sessões, tokens, secrets e dados financeiros.
3. **MÉDIO — ausência de trilha específica para exportação.** Eventos `data_export.requested` e `data_export.completed` passam a ser persistidos com tenant, usuário, severidade e UTC.
4. **MÉDIO — ausência de modelo completo para governança.** Migration incremental cria histórico versionado de consentimentos, exportações e exclusões com estados, constraints e índices.
5. **BAIXO — comunicação da tela não detalhava o conteúdo.** O painel agora informa categorias exportadas e exclusões de segurança, oferecendo download JSON e solicitação CSV.

## Controles existentes confirmados

- Cookies HttpOnly/SameSite e Secure fora de desenvolvimento; validação de sessão persistida.
- Antiforgery global para métodos inseguros, com tokens explícitos nos formulários de privacidade.
- Políticas Admin/SuperAdmin e rotas administrativas autenticadas.
- Middleware de vínculo de cliente e bloqueio de conta.
- Rate limiting no assistant e notificações; tratamento global de exceções e HSTS em produção.
- SQL do fluxo alterado é parametrizado; nenhum segredo foi introduzido em código ou logs.

## Riscos residuais e recomendações

- Os repositórios legados de hábitos, conclusões, suporte e cobrança ainda possuem métodos que recebem somente `user_id`; embora IDs sejam globais e controllers façam binding, recomenda-se migrar todos para assinatura `(client_id,user_id)`.
- O processamento assíncrono de CSV, anonimização e exclusão física exige worker operacional, política de retenção jurídica e armazenamento temporário criptografado; esta entrega mantém o pedido revisável e cria o schema seguro para evolução.
- Rate limits de login e suporte devem ser harmonizados em uma política particionada por IP/conta após teste de carga.
- A CSP deve permanecer em modo compatível com os assets atuais e evoluir para nonces antes de remover estilos inline legados.

## Conclusão

O caminho de privacidade ganhou isolamento explícito de tenant, portabilidade real e auditada, schema incremental para consentimento/versionamento e lifecycle de exclusão. Não foram encontrados segredos novos ou SQL interpolado nos arquivos alterados.
