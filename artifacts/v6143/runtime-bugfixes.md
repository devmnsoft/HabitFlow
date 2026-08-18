# Bugs observados v6.14.3

| Erro | Causa | Arquivo | Correção | Evidência |
|---|---|---|---|---|
| Security scan classificou exemplo como segredo | Exemplo continha chave `Password` literal | `scripts/validation/run-release-candidate-local-windows.ps1` | Exemplo passou a receber a conexão por variável de ambiente | `npm run security:scan` verde após a correção |

Nenhuma falha de runtime da aplicação pôde ser observada porque PowerShell, .NET e PostgreSQL não existem neste ambiente.
