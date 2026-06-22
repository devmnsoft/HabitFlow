# UX Review v2.3.4-Audit-Fix-Callable-Cache-Layout

## Áreas revisadas
- Landing page, autenticação, dashboard, modal de hábito, progresso, perfil, suporte, chatbot, Admin Geral e PWA.

## Melhorias aplicadas
- Diagnóstico Técnico do Admin Geral recebeu controles claros para limpar cache PWA, desregistrar service worker e recarregar a aplicação.
- Fluxo de instalação PWA só intercepta `beforeinstallprompt` quando existe botão real de instalação e exibe o botão no momento correto.
- Service worker ganhou cache versionado e política conservadora para evitar JavaScript antigo e nunca cachear Firebase/Functions.
- Mensagens de Functions e logger foram mantidas amigáveis, sem erro técnico bruto para usuário comum.

## Checklist mobile
- Botões continuam com área clicável mínima.
- Card de instalação PWA empilha em telas pequenas.
- Chatbot mantém aviso de dados sensíveis e ação de fechar/minimizar.
- Admin Geral concentra ações técnicas em botões pequenos e responsivos.

## Pendências futuras
- Revisão visual assistida por navegador autenticado em todos os perfis de usuário.
- Teste de instalação PWA em Chrome Android/Safari iOS reais.
- Auditoria de contraste automatizada com Lighthouse/axe em ambiente com dependências completas.
