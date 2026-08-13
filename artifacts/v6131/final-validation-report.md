# Relatório final de validação — v6.13.1

## Identificação e ambiente

- **SHA inicial:** `0b3d3e81b1f5011c6bf86633f862336ba59e7b7b`
- **SHA final:** será o commit desta entrega, registrado no PR/Git após a geração deste relatório.
- **.NET:** Bloqueado — SDK/runtime ausente.
- **PostgreSQL:** Bloqueado — `psql` e servidor ausentes.
- **Node:** Aprovado — v24.15.0.
- **npm:** Aprovado — 11.4.2.

## Matriz objetiva

| Validação | Status | Evidência |
|---|---|---|
| Build .NET Release | Bloqueado | `dotnet` ausente no ambiente local; job CI criado |
| Publish .NET Release | Bloqueado | `dotnet` ausente no ambiente local; job CI criado |
| Migration — banco novo | Bloqueado | PostgreSQL/psql ausentes; script e job CI criados |
| Migration — banco existente | Bloqueado | PostgreSQL/psql ausentes; script e job CI criados |
| Migration — rerun | Bloqueado | PostgreSQL/psql ausentes; script e job CI criados |
| Startup localhost:5097 | Bloqueado | runtime ASP.NET ausente |
| Rotas públicas | Não executado | aplicação não pôde ser iniciada |
| Rotas autenticadas | Não executado | aplicação/sessão indisponíveis |
| Jornada de usuário novo | Não executado | runtime indisponível |
| Jornada de usuário com dados | Não executado | runtime indisponível |
| Planos e integridade comercial runtime | Não executado | banco e runtime indisponíveis |
| Busca global runtime | Não executado | navegador/runtime indisponíveis |
| npm security:scan | Aprovado | execução local sem achados |
| npm test existente | Aprovado | testes de segurança existentes passaram |
| npm audit --omit=dev | Aprovado | zero vulnerabilidades |
| Sintaxe dos oito assets JS | Aprovado | `node --check` passou em todos |
| Responsividade/screenshots | Bloqueado | aplicação e navegador autenticado indisponíveis; nenhuma imagem falsa criada |

## Alterações e problemas encontrados

Foram adicionados scripts operacionais seguros para validação Windows, migrations em três cenários, provisionamento por fluxo HTTP real, seed local idempotente e smoke autenticado. O scanner de segurança inicialmente rejeitou uma connection string completa no YAML; o workflow foi corrigido para compô-la somente no processo efêmero de CI e o scanner passou no rerun.

Não foi observado erro de UI/runtime porque o runtime não pôde ser iniciado. Não houve alteração visual sem observação real. Não foram criados testes, specs, credenciais persistentes ou binários.

## Capturas

**Não executado:** `artifacts/v6131/screenshots/` não foi fabricado, pois nenhum viewport foi aberto.

## Pendências reais

1. Executar o workflow em GitHub Actions e anexar seus artifacts.
2. Executar `validate-local-windows.ps1` em Windows com .NET 10, Git Bash e PostgreSQL.
3. Provisionar identidade efêmera e executar seed/smoke autenticado.
4. Validar jornadas, planos e nove viewports em navegador real; capturar apenas imagens reais.

**Decisão da release neste ambiente: Bloqueado**, não Aprovado.
