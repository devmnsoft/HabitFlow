# Ordem CSS v6.5.1

1. Bootstrap 5.3.3;
2. `site.css` (tokens e visual legado ativos);
3. `layout-stabilization.css` (geometria conservadora do shell);
4. exatamente uma folha contextual: `public.css`, `personal.css`, `account.css` ou `platform.css`;
5. `accessibility.css`;
6. `responsive.css`;
7. `print.css`, somente em mídia de impressão;
8. seção Razor `Styles` opcional.

`tokens.css`, `base.css` e `components.css` permanecem experimentais e inativos até migração completa. Assim, `--hf-bg`, `--hf-surface`, `--hf-text`, `--hf-primary`, raios e sombras têm `site.css` como fonte ativa única. A consolidação futura deve remover gradualmente regras do legado, com comparação visual por etapa, e não reativar as três folhas em bloco.
