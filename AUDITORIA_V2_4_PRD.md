# Auditoria v2.4 PRD IIS Production Stabilization

## Buscas obrigatórias
Executadas durante a estabilização:
- `rg -n "marcadores de conflito" .`
- `rg -n "cloudfunctions.net|FUNCTIONS_BASE_URL|CLOUD_FUNCTIONS_URL|API_BASE_URL" assets dist public . --glob '!**/*.md'`
- `rg -n "getFunctions|httpsCallable" assets --glob '!assets/js/functions-client.js'`
- `rg -n "onRequest|https\.onRequest" functions`
- `rg -n "sourceMappingURL|\.map" dist public assets`
- `rg -n "TELEGRAM_BOT_TOKEN|MERCADOPAGO_ACCESS_TOKEN|STRIPE_SECRET_KEY|AI_API_KEY|PRIVATE_KEY|BEGIN PRIVATE KEY|serviceAccount|firebase-adminsdk" .`

## Riscos encontrados e correções
- Service worker antigo `habitflow-v2-3-5`: atualizado para `habitflow-v2-4-prd` com limpeza de caches antigos e bypass para Firebase/Google/Functions.
- Logger remoto podia registrar `app_loaded` antes do bootstrap: app agora só envia eventos remotos após settings, Auth e bootstrap `healthCheck` + `logSystemEvent`.
- Produção IIS não tinha `web.config`: criado com SPA fallback, MIME types, bloqueio de arquivos sensíveis, headers e HTTPS opcional.
- Seeds PRD inexistentes: adicionados scripts controlados com confirmação explícita.

## Status
- Functions internas verificadas por `scripts/verify-functions-shape.js`.
- `paymentWebhook` preservado como `onRequest`.
- Build PRD configurado para `dist/` sem source maps.

## Pendências operacionais
- Executar deploy real Firebase/IIS em ambiente autorizado.
- Configurar domínio final em Firebase Auth, App Check, CSP e `APP_ALLOWED_ORIGINS`.
- Registrar smoke test final em `PRD_DEPLOY_LOG.md`.
