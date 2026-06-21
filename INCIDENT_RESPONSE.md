# Resposta a Incidentes — HabitFlow v2.1-SecurityOps

## O que é incidente
Evento que ameaça confidencialidade, integridade, disponibilidade, privacidade, cobrança ou operação do HabitFlow.

## Severidades
- **low**: impacto limitado, sem dados sensíveis.
- **medium**: falha com impacto em usuário ou operação.
- **high**: abuso, indisponibilidade parcial, risco LGPD ou pagamento inconsistente.
- **critical**: token exposto, vazamento de dados, acesso admin indevido, perda de dados ou exploração ativa.

## Identificação e alerta
- Eventos críticos são registrados em `systemAuditLogs` e podem acionar Telegram.
- O Admin Geral exibe painel de Segurança, eventos suspeitos e solicitações LGPD.

## Fluxo
1. Criar incidente em `securityIncidents`.
2. Coletar logs, usuário, origem, horário e ação afetada.
3. Conter: revogar tokens, pausar integração, limitar Functions ou bloquear deploy.
4. Erradicar causa raiz.
5. Recuperar dados/serviço a partir de backup validado, se necessário.
6. Comunicar titulares/partes afetadas quando aplicável.
7. Fechar com causa raiz, resolução e ações tomadas.

## Checklist pós-incidente
- [ ] Logs preservados.
- [ ] Secrets rotacionados, se aplicável.
- [ ] Regras/Functions corrigidas.
- [ ] Backup testado.
- [ ] Usuários impactados avaliados.
- [ ] Documentação atualizada.

## Exemplos
- Token exposto: revogar token, remover histórico, rodar `npm run security:scan`.
- Tentativa admin indevida: revisar `unauthorized_admin_attempt`, validar `ADMIN_EMAILS`.
- Erro crítico Firestore: verificar Rules, índices e Functions.
- Abuso de chatbot: conferir `prompt_injection_attempt` e rate limit.
- Vazamento de dados: conter, avaliar LGPD e comunicação.
- Pagamento inconsistente: reconciliar `billingEvents` e plano do usuário.
