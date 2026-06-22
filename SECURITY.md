# Segurança do HabitFlow 2.0-Security

Por limitação natural da web, qualquer JavaScript executado no navegador pode ser inspecionado. O HabitFlow reduz exposição usando build de produção, minificação, ofuscação leve, remoção de source maps e, principalmente, movendo lógica sensível para Firebase Functions com validações de segurança no backend.

## Frontend
- Contém apenas configuração pública do Firebase client.
- Não deve conter tokens de Telegram, chaves de IA, credenciais de pagamento ou service accounts.
- Usa `APP_ENV` para reduzir logs técnicos e botões de diagnóstico em produção.

## Backend e Firestore
- Coleções globais (`systemAuditLogs`, `adminAuditLogs`, `billingEvents`, `supportTickets`, `systemSettings`) são bloqueadas para o client.
- Ações sensíveis usam Firebase Functions com autenticação, admin backend e rate limit básico.

## App Check
Consulte `APP_CHECK.md`. Ative enforcement somente após validar localhost, Hosting, Auth, Firestore e Functions.

## LGPD e logs
Logs são sanitizados para remover senha, token, secret, CPF, cartão, payload bruto e chaves privadas. Reporte vulnerabilidades para comercial@mnsoft.com.br.

## v2.1-SecurityOps
- Adicionada camada operacional de segurança com CI, scanners de secrets/dist, validação de Firebase config e Firestore Rules.
- Admin Geral passa a ter painel de Segurança, eventos suspeitos, incidentes e solicitações LGPD.
- Functions críticas usam rate limit e auditoria administrativa backend; dados globais continuam protegidos por Rules e acessados via Functions.
- Produção deve usar `npm run build`, `npm run security:scan`, `npm run security:dist`, `npm run security:rules`, `npm run security:firebase` e `npm test` antes de publicar.
- Source maps, `.env`, Functions, `node_modules` e documentação interna não devem ser publicados no Hosting.

## v2.2-Production

- Secrets de Telegram, e-mail e pagamento devem existir somente em Firebase Functions.
- App Check deve iniciar em monitoramento e avançar para enforcement parcial/total após validação.
- LGPD, backups e deploys são manipulados por Functions administrativas.
- Excluir dados de usuário exige simulação (`dryRun`) e confirmação explícita `CONFIRMAR_EXCLUSAO`.

## Admin Global v2.3

Consultas globais e ações administrativas são executadas por Firebase Functions. Firestore Rules bloqueiam `adminAuditLogs`, `systemAuditLogs`, `adminUserNotes`, `billingEvents` e dados globais para clientes comuns.


## v2.4 PRD
Não publicar source maps, `.env`, `functions/`, `node_modules`, tokens Telegram, chaves IA, Mercado Pago ou service accounts. O `web.config` bloqueia arquivos sensíveis no IIS e `security:dist` valida o pacote de produção.
