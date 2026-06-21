# CHATBOT.md — Assistente HabitFlow v1.8

O Assistente HabitFlow é um chatbot local baseado em regras, intents e respostas pré-configuradas. Nesta versão ele não usa API externa de IA e não possui chave no frontend.

## Objetivo
Responder dúvidas sobre uso do HabitFlow: criação/edição/arquivamento de hábitos, streak, histórico de 30 dias, plano gratuito, Premium futuro, privacidade, suporte e dados comerciais públicos da MNSOFT.

## Base de conhecimento e intents
A base fica em `assets/js/chatbot-knowledge.js` e cobre: `greeting`, `create_habit_help`, `streak_help`, `plan_help`, `premium_help`, `privacy_help`, `support_help`, `whatsapp_help`, `bug_report`, `mnsoft_info` e `unknown`.

## Segurança
O assistente bloqueia pedidos sobre tokens, chaves, logs internos, burlar segurança, dados de outros usuários, CPF, cartão, senhas e stack traces. A resposta padrão é segura e orienta o usuário para suporte oficial.

## Logs
Não salva conversas completas por padrão. Registra apenas intent, tamanho da mensagem, bloqueio e status. Relatos de bug podem salvar uma descrição sanitizada quando o usuário confirma.

## Evolução futura
Na v1.9, a IA poderá ser integrada somente via backend seguro, com prompt versionado, moderação, limite de tokens, logs de segurança e fallback humano.
