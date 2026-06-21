# Observabilidade do HabitFlow

A versão 1.7 adiciona uma camada profissional de observabilidade, auditoria, monitoramento e alertas do HabitFlow.

## Eventos monitorados

- Login, cadastro e logout.
- Aceite de termos e privacidade.
- Início e conclusão do onboarding.
- Criação, edição, arquivamento, restauração, conclusão e remoção de conclusão de hábitos.
- Visualizações de perfil, progresso e Premium.
- Interesse Premium, clique de checkout e retornos de pagamento.
- Instalação PWA.
- Abertura do Admin Geral.
- Performance: `app_loaded`, `dashboard_loaded` e `habits_loaded`.

## Erros capturados

- `window.onerror` para erros JavaScript globais.
- `unhandledrejection` para promises rejeitadas.
- Falhas Firebase Auth, Firestore, Functions, PWA/service worker, checkout e webhook.

## systemAuditLogs

Coleção global protegida escrita por Functions. Contém tipo, severidade, origem, usuário, ação, mensagem, metadados sanitizados, versão, ambiente, leitura admin, status Telegram e `errorFingerprint`.

## users/{uid}/usage/events

Registro pessoal do usuário, lido e escrito apenas pelo próprio usuário conforme Firestore Rules. Serve para histórico de uso pessoal e diagnósticos leves.

## adminAuditLogs

Coleção global bloqueada no client. Registra chamadas administrativas como leitura do painel, marcação de log como lido e alterações manuais de plano.

## Painel Admin Geral

A aba **Admin Geral** aparece apenas para e-mails listados no frontend para fins visuais, mas toda autorização real acontece nas Functions com `ADMIN_EMAILS` seguro. O painel exibe resumo, eventos recentes, bugs, atividades, status do Telegram e botão de teste.

## Logs pessoais vs logs globais

- Logs pessoais: ficam em `users/{uid}/usage/events` e pertencem ao usuário.
- Logs globais: ficam em `systemAuditLogs`, são protegidos e acessados apenas por Functions administrativas.

## Cuidados LGPD e sanitização

Metadados removem chaves com senha, tokens, cartão, CVV, CPF, documentos, secrets e payloads brutos. Mensagens e strings são limitadas para reduzir coleta excessiva.

## Retenção de logs

Constantes documentadas no backend:

- `LOG_RETENTION_DAYS = 90`
- `ERROR_LOG_RETENTION_DAYS = 180`

A versão atual não apaga automaticamente. Uma scheduled function futura deve executar a limpeza.

## Próximas melhorias

- Agrupamento de erros por assinatura.
- Dashboard com gráficos.
- Alertas por e-mail.
- Exportação CSV.
- Integração futura com Sentry.
- Métricas agregadas de conversão.
