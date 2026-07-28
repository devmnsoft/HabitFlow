# Variantes de navegação v6.5.1

| Variante | Uso | Contrato |
|---|---|---|
| `PublicTop` | visitante | links compactos, sem descrição permanente |
| `PersonalTop` | pessoa autenticada | Hoje, Hábitos, Objetivos, Progresso e Relatórios |
| `AccountSidebar` | conta desktop | descrição opcional e sete destinos sujeitos a acesso |
| `PlatformSidebar` | operação desktop | grupos Visão, Gestão, Financeiro, Operação e Controle |
| `MobileBottom` | pessoal até 575 px | máximo de cinco itens e safe-area |
| `MobileDrawer` | conta/plataforma mobile | offcanvas com foco, Escape e backdrop geridos pelo Bootstrap |

`NavigationItem.IsEnabled` controla publicação; `IsCurrent` controla apenas `aria-current` e destaque. Permissões e features continuam avaliadas por `NavigationAccessEvaluator`. Nenhum menu é hardcoded em View de página.
