# Branding MNSOFT

A logo oficial deve ser adicionada manualmente em `src/HabitFlow.Web/wwwroot/brand/mnsoft/logo-mnsoft-oficial.png` pelo cliente ou responsável autorizado. O Codex não deve commitar imagens binárias.

Quando a imagem oficial existir, o ViewComponent renderiza a imagem sem distorção, com `max-width`, `height: auto` e `object-fit: contain`. Quando não existir, o sistema exibe apenas uma assinatura visual temporária textual para evitar imagem quebrada. Esse fallback não é chamado de logo oficial.
