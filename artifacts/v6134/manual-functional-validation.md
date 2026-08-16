# Validação funcional manual v6.13.4

| Rota | Ação | Resultado | Erro/correção | Pendência |
|---|---|---|---|---|
| `/weekly-review` | Revisar composição Razor, métricas, categorias, hábitos, objetivos e CTAs | Contratos e URLs conferidos estaticamente | Sem erro estático encontrado | Execução HTTP bloqueada pela ausência do SDK .NET |
| `/weekly-review/{data}/complete` | Conferir antiforgery, idempotência e auditoria | Formulário contém token; use case preserva idempotência e registra evento amigável | — | Exercitar com PostgreSQL |
| `/habits/{id}` | Ajustar para dias úteis ou 3x/semana | Endpoint valida escopo, atualiza apenas agenda futura e preserva conclusões | Feedback de sucesso/erro incluído | Exercitar com PostgreSQL |
| `/habits/{id}` | Ajustar duração para 5 ou 10 minutos | Endpoint valida intervalo, não cria hábito e registra auditoria | Feedback de sucesso/erro incluído | Exercitar com PostgreSQL |
| `/dashboard` | Inspeção de regressão | Nenhuma alteração nesta entrega | — | Integração da recomendação central permanece para evolução posterior |
| `/my-day` | Inspeção de regressão | Nenhuma alteração nesta entrega | — | Integração da recomendação central permanece para evolução posterior |
| `/goals/{id}` | Dados de objetivo na revisão | Objetivos e vínculo com hábitos são consultados com escopo de conta/pessoa | — | Progresso automático não foi alterado nesta entrega |
| `/reminders` | Presença de lembrete usada pela recomendação | Apenas lembretes ativos evitam sugestão duplicada | — | Sugestão de horário/conflito permanece pendente |
| `/reports` | CTA da revisão | Rota existente usada; nenhuma promessa nova de exportação | — | Insights expandidos permanecem pendentes |
| `/account/plan/usage` | CTA de recomendação de limite | Rota existente e segura utilizada pelo motor | — | Exposição do limite requer contexto de plano na superfície consumidora |
