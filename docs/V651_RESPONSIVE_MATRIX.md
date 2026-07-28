# Matriz responsiva v6.5.1

| Largura | Header/menu | Conteúdo | Validação |
|---:|---|---|---|
| 320, 360, 390, 430 | CTA reduzido, drawer ou bottom nav | uma coluna | overflow ≤ client + 1 |
| 768 | navegação superior recolhida | cards fluidos | tabelas com scroll próprio |
| 1024 | navegação compacta; sidebar desktop | 1180/1440 limitados à viewport | sem sobreposição |
| 1280, 1440 | shell completo | grids responsivos | sidebar sticky |
| 1920 | conteúdo centralizado | máximo contextual | footer abaixo do conteúdo |

Playwright automatiza 320, 390, 768, 1024, 1440 e 1920. As larguras 360, 430 e 1280 fazem parte da inspeção manual/DevTools e da expressão CSS coberta pelos mesmos breakpoints.
