# Auditoria de assets v6.12.0

## Resultado

`_Layout.cshtml` referencia somente o header atual (`header-v4.css/js` e `navigation-v4.css`). `app-header-v2.css/js` e `navigation-v2.css` permanecem no repositório por compatibilidade com testes históricos, mas não são carregados no runtime e portanto não sobrescrevem v4.

A ordem ativa preserva fundações antes de componentes: Bootstrap → site/design/forms → feedback → layout/navigation/shell → busca/header/navigation v4 → contexto → acessibilidade/responsivo. JavaScript carrega infraestrutura antes de busca/header e scripts de tour.

## Decisões

- Nenhuma alteração visual no header foi feita sem browser real.
- Nenhuma referência duplicada exata foi encontrada.
- `feedback.css`/`feedback-system.js` continuam como base; `feedback-v5.css/js` fornece o contrato atual de toast/confirmação segura.
- `guided-tour.js` e `guided-tour-v4.js` têm responsabilidades legadas/evolutivas e permanecem até uma validação funcional autenticada comprovar que podem ser fundidos.
- Os assets obsoletos v2 não foram apagados porque testes .NET históricos ainda os leem diretamente; a remoção deve ocorrer em mudança separada com atualização desses contratos.

## Guardas

`validate-local.ps1` executa `node --check` em header, feedback, busca, navegação e tours. `security-ci.yml` rejeita diálogos nativos, atribuições a `innerHTML`, secrets e binários indevidos. `playwright-ci.yml` captura erros de console, overflow e overlaps com screenshots reais.
