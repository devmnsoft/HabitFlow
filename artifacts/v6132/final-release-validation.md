# Validação final de release v6.13.2

- SHA inicial: `d1506602377f6f525ddcfbdf87b6b34112324507`
- SHA final: commit que contém este relatório (consultar histórico Git)
- Workflow run URL: **Não executado** — autenticação GitHub indisponível
- Release candidate: **Bloqueado** (não aprovado)

| Área | Status | Evidência |
|---|---|---|
| build-dotnet | Não executado | SDK .NET ausente; download recebeu HTTP 403 |
| publish | Não executado | Depende do build |
| frontend-security | Aprovado | `npm install`, scanner, testes existentes e audit executados localmente; 0 vulnerabilidades |
| postgres-migrations | Não executado | PowerShell, psql e PostgreSQL ausentes |
| artifact-report remoto | Não executado | Sem workflow run |
| scripts Windows | Aprovado por revisão / Bloqueado por execução | Relatório específico de scripts |
| runtime smoke | Bloqueado | Runtime e banco indisponíveis |
| jornadas | Não executado | Runtime autenticado indisponível |
| integridade comercial | Não executado | Runtime/banco indisponíveis |

## Correções aplicadas

- Workflow e artifacts promovidos para v6.13.2.
- Credencial efêmera do serviço PostgreSQL montada em runtime no passo PowerShell.
- Relatório PostgreSQL alinhado ao path de upload do workflow.
- Nome de banco temporário validado antes de interpolação SQL.
- Helpers receberam exemplos de uso e outputs v6.13.2.
- Seed demo agora recusa execução fora de `Development`, sem bypass.

## Pendências reais

1. Publicar a branch e disparar `workflow_dispatch` ou abrir o PR em uma sessão GitHub autenticada.
2. Registrar URL, timestamps, conclusions, logs e artifacts do run real.
3. Corrigir qualquer falha revelada pelo runner e repetir até conclusão verificável.
4. Executar smoke autenticado, jornadas e integridade comercial em ambiente operacional.

Nenhum teste foi criado/alterado, nenhum segredo ou binário foi adicionado e a release **não está aprovada**.
