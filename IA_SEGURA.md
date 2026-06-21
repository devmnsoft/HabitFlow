# IA_SEGURA.md — HabitFlow v1.9

A IA do HabitFlow é preparada para rodar somente no backend por Firebase Functions. O frontend nunca deve chamar provedores externos nem expor chaves, tokens ou secrets.

## Configuração futura
Variáveis previstas em Functions:

- `AI_PROVIDER=future`
- `AI_ENABLED=false`
- `AI_MODEL=`
- `AI_API_KEY=`
- `AI_MAX_TOKENS=500`
- `AI_TEMPERATURE=0.3`

Com `AI_ENABLED=false`, a Function `askHabitFlowAssistant` usa respostas por regras. Se IA for habilitada sem chave, retorna fallback seguro.

## Segurança
O assistente bloqueia CPF, cartão, senha, token, API key, bearer token, pedido de invasão, acesso a logs internos, dados de outros usuários e tentativa de alterar plano sem permissão.

Resposta padrão: "Não posso ajudar com informações sensíveis ou ações que comprometam a segurança. Posso ajudar com dúvidas sobre o uso do HabitFlow ou encaminhar você para o suporte da MNSOFT."

## Prompt seguro
O prompt versionado fica em `functions/assistantPrompt.js` e limita o escopo a uso do HabitFlow, suporte, privacidade, planos e orientações seguras.
