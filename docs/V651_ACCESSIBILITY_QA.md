# Acessibilidade v6.5.1

- Skip link aponta para `main#conteudo`, que recebe foco.
- Header, main, aside, nav e footer têm landmarks/rótulos.
- Rota atual usa `aria-current="page"`; menus Bootstrap mantêm `aria-expanded` e `aria-controls`.
- Offcanvas oferece botão rotulado, Escape, backdrop, focus trap e retorno ao acionador pelo Bootstrap.
- Perfil tem nome acessível, e-mail com quebra segura e ações agrupadas.
- Foco visível, alto contraste, fonte ampliada e redução de movimento continuam ativos.
- Bottom navigation respeita safe-area e limita-se a cinco itens.

O conteúdo das Views deve manter exatamente um `h1`; Playwright verifica ao menos um `h1` visível nas rotas públicas e a auditoria de CI deve apontar duplicidades por rota.
