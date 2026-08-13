# Inventário do ambiente — v6.13.0

- **SHA inicial:** `3014644d0fd3fe0f6f0c1486b17f709277db0057`
- **Branch de trabalho:** `feature/v6130-real-validation-final-polish-release-candidate` (criada localmente a partir do HEAD solicitado; o checkout inicial estava em `work`)
- **Sistema:** container Linux, diretório `/workspace/HabitFlow`, UTC
- **.NET SDK/runtime:** indisponível (`dotnet: command not found`)
- **PostgreSQL/psql:** indisponível (`psql: command not found`)
- **Docker/Compose:** indisponível (`docker: command not found`)
- **Node.js:** `v24.15.0`
- **npm:** `11.4.2`
- **Banco usado:** nenhum; não havia cliente/servidor PostgreSQL acessível neste ambiente
- **Porta reservada:** `5097`

## Limitações reais

Conforme a regra da etapa, a parte .NET foi interrompida quando a ausência do SDK foi confirmada. Assim, clean, restore, build, publish e startup não foram executados nem declarados aprovados. Sem `psql` e sem Docker, migrations e consultas de sanidade também não puderam ser executadas. Como a aplicação não pôde subir, não houve sessão autenticada, persistência real ou captura honesta dos viewports.
