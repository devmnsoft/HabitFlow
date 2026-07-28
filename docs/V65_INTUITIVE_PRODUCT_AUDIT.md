# Auditoria real — v6.5 Intuitive Experience

Data: 2026-07-28. Escopo avaliado no código-fonte, sem presumir disponibilidade de infraestrutura externa.

## Classificação

| Área | Estado inicial | Evidência e encaminhamento v6.5 |
|---|---|---|
| Home | parcial | Estrutura funcional, porém texto pouco humano; hero reescrito. |
| Demonstração | parcial | Fluxo existente, sem cobertura visual completa nesta entrega. |
| Login e Cadastro | parcial | Fluxos existem; cadastro ainda precisa revisão integral de microcopy. |
| Planos e Billing | confuso / legado | Acesso não era global e retorno citava termos técnicos; rota “Meu plano” e mensagens humanas adicionadas. |
| Dashboard | parcial | Serviços persistidos existem, mas a auditoria encontrou métricas que ainda exigem validação por ocorrências previstas. |
| Hábitos | parcial | CRUD existente; preservação integral do template demanda evolução de modelo/migration. |
| Biblioteca | não intuitivo | Seleção inferia ícone pelo nome e catálogo era pequeno; inferência removida e evolução de dados registrada como trabalho obrigatório. |
| Objetivos | parcial | CRUD e progresso existem; associação e conclusão automática precisam teste PostgreSQL. |
| Progresso, Calendário e Relatórios | parcial / sem teste | Rotas existem; cálculo real e exportações precisam validação de integração. |
| Lembretes e Notificações | parcial | Estrutura e migrations existentes; jornada integrada ainda parcial. |
| Perfil e Sua conta | confuso | “Meu plano” não era visível; contexto de conta e CTA global adicionados. |
| Suporte | parcial | Fluxo presente, linguagem ainda requer inventário completo. |
| Admin e Super Administrador | legado | Contexto próprio criado; telas e autorização granular permanecem parciais. |
| Permissões | apenas estrutura | Tabelas existem; integração completa em handlers continua necessária. |
| Cobrança | legado | Há serviços antigos por usuário; migração total para conta não foi declarada como concluída. |
| PWA | parcial | Manifest e scripts existem; auditoria Lighthouse não executada. |
| Mobile | parcial / sem teste | Regras 430px adicionadas; matriz completa requer Playwright. |
| Textos | parcial / corrigido | Guia criado e pontos críticos reescritos; inventário total permanece aberto. |
| Ícones e ilustrações | parcial | SVGs locais existem; catálogo de 60 ícones ainda não comprovado. |
| Acessibilidade | parcial | Foco, movimento reduzido, impressão e forced colors reforçados; auditoria assistiva completa pendente. |
| Testes | parcial | Suíte existente; resultados dos comandos devem ser registrados no PR, sem alegações antecipadas. |

## Decisões de arquitetura

Quatro layouts de contexto envolvem um shell compartilhado, que continua sendo a fonte única de head, scripts, mensagens, modais, identidade e footer. A navegação passa a ser produzida por serviço/ViewComponent; Views não mantêm arrays de links. “Meu plano” é oferecido a toda sessão autenticada e a Gestão da Plataforma apenas a quem possui papel ou permissão compatível.

## Riscos não mascarados

Esta auditoria não classifica como completo aquilo que só possui tabela, controller ou View. Biblioteca com 160 conteúdos editoriais, checkout por conta, métricas de ocorrência, exports, RBAC e criação segura de Super Administrador exigem implementação e testes de integração adicionais antes de aceite de produção.

## Comandos de verificação

Os comandos exigidos foram executados no ciclo desta entrega; o resultado factual (sucesso, falha ou limitação do ambiente) deve constar no resumo do Pull Request e na resposta final, nunca ser inferido por este documento.
