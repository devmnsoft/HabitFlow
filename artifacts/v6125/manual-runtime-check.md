# Validação manual de runtime — v6.12.5

Data: 2026-08-13 (UTC)

> O runtime .NET não está instalado neste ambiente. Por isso, nenhuma rota autenticada foi declarada como validada manualmente. A tabela preserva a distinção entre inspeção estática e execução real.

| Rota | Ação | Resultado | Erro encontrado | Correção aplicada | Pendência |
|---|---|---|---|---|---|
| `/habits/create` | Abrir editor | Inspeção estática | Categoria sem sugestões; objetivo era ID manual | `datalist`, seleção de objetivos e prévia funcional | Abrir com usuário/BD reais |
| `/habits` | Listar | Não executado | — | — | Validar em runtime |
| `/habits/{id}` | Detalhar | Não executado | Possível overlay restaurado pelo navegador | Estado inicial dos overlays reforçado | Validar em runtime |
| `/goals` | Listar | Inspeção estática | Caminho relativo do partial não resolvia a subpasta | Caminho explícito `Partials/_GoalProgressBar` | Validar em runtime |
| `/goals/create` | Abrir editor | Não executado | — | — | Validar em runtime |
| `/goals/{id}` | Detalhar | Inspeção estática | Progresso não tolerava modelo/target vazio | Partial defensivo e acessível | Validar em runtime |
| `/dashboard` | Abrir | Não executado | — | — | Validar em runtime |
| `/my-day` | Abrir | Não executado | — | — | Validar em runtime |
| `/plans` | Abrir e alternar ciclo | Não executado | — | — | Validar em runtime |

## Checks de interface pendentes

Os viewports 1440×900, 1366×768, 1024×768, 768×1024, 430×932, 390×844, 360×800 e 320×568 exigem navegador, servidor, autenticação e PostgreSQL disponíveis. CRUD de hábitos, objetivos, lembretes, notificações e privacidade também permanece pendente por essa limitação ambiental.
