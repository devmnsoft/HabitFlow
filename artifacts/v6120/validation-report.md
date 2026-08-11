# HabitFlow v6.12.0 — execução neste workspace

Gerado em 2026-08-11 (UTC). Este arquivo registra resultados reais; `validate-local.ps1` o substitui com o relatório completo em um host provisionado.

| Etapa | Resultado | Evidência |
|---|---:|---|
| npm ci | PASS | 316 pacotes instalados |
| npm run security:scan | PASS | `Security scan OK (projeto)` após retirar connection string literal do workflow |
| npm test | PASS | Firestore, Firebase e testes unitários de segurança OK |
| npm audit --omit=dev | PASS | 0 vulnerabilidades |
| node --check (JS principal) | PASS | header, feedback, busca, navegação e tours analisados |
| YAML dos workflows | PASS | cinco arquivos carregados pelo parser Ruby/Psych |
| descoberta Playwright | PASS | 832 testes descobertos; este resultado não é contabilizado como execução browser |
| instalação Chromium | FAIL | todos os endpoints oficiais retornaram HTTP 403 pelo proxy deste workspace |
| dotnet clean/restore/build/test/publish | FAIL | executável `dotnet` não existe e o download oficial retornou HTTP 403 |
| migrations novo/existente/rerun | FAIL | `psql` não existe; repositórios APT retornaram HTTP 403 |
| aplicação em localhost:5097 | FAIL | depende do SDK e PostgreSQL ausentes |
| Playwright real e screenshots | FAIL | browser e aplicação indisponíveis; nenhum screenshot foi declarado ou produzido |

A release candidate somente deve ser aprovada quando `release-candidate.yml` concluir os quatro gates. O pipeline não converte skips autenticados em sucesso: exige o secret efêmero `HABITFLOW_AUTH_STORAGE_B64`.
