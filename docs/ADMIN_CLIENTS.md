# Admin clientes

O módulo administrativo de clientes permite gerenciar empresas, pessoas e contas comerciais do HabitFlow nas rotas protegidas por perfil `Admin`.

## Rotas

- `GET /admin/clients`: lista clientes com filtro por nome, e-mail, documento, status e plano.
- `GET /admin/clients/create`: exibe `CreateClientRequest`.
- `POST /admin/clients/create`: cria cliente, audita a ação e exibe feedback de sucesso ou validação.
- `GET /admin/clients/{id}`: exibe `ClientDetailDto` com dados, métricas e usuários vinculados.
- `GET /admin/clients/{id}/edit`: exibe `UpdateClientRequest`.
- `POST /admin/clients/{id}/edit`: atualiza cliente, audita a ação e retorna feedback claro.
- `POST /admin/clients/{id}/activate`: ativa cliente.
- `POST /admin/clients/{id}/deactivate`: desativa cliente mantendo histórico.
- `POST /admin/clients/{id}/block`: bloqueia cliente com confirmação e feedback modal.

## Arquitetura

- DTOs e filtros ficam em `HabitFlow.Application`.
- Regras de criação, atualização, consulta, auditoria e tratamento amigável de falhas ficam em `ClientService`.
- Persistência usa Dapper em `ClientRepository`.
- SQL usa schema explícito `habitflow`, especialmente `habitflow.clients`.
- Views Razor recebem apenas tipos existentes e públicos da camada Application.

## UX e feedback

As telas exibem mensagens por `ApplicationFeedbackService` e `_FeedbackBridge.cshtml`, usando `TempData` estruturado para toasts e modais sem expor stack trace, SQL ou detalhes do PostgreSQL para o usuário comum.
