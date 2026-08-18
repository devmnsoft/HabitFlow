# Relatório final da Release Candidate — v6.14.6

- SHA inicial: `bd2ffddb6e2adf1b53d0e307e0062c91fc724604`.
- SHA final: será o commit que incorpora esta evidência.
- Ambiente Windows: **não disponível**; executor efetivo foi Linux x86_64.
- Build/publish Windows: não executado.
- Migrations PostgreSQL: não executadas.
- EXPLAIN: não executado.
- Startup/log runtime: não executado.
- Smoke público: não executado.
- Smoke autenticado: não executado.
- Biblioteca: não validada em navegador real.
- Jornada MVP: não validada.
- Regras Free/Ritmo/Evolução: não validadas.
- Mobile: não validado; nenhuma screenshot foi criada.
- Bug corrigido: o runner v6.14.5 foi atualizado para a release v6.14.6 e agora rejeita explicitamente ambiente não Windows, agrega ferramentas ausentes e emite artifacts com os nomes v6.14.6.
- Secrets/publish/binários: nenhum foi criado ou incluído.

## P0s pendentes

Todos os gates de runtime listados acima permanecem P0 até uma execução em Windows real com PostgreSQL acessível. Não há evidência suficiente para indicar o primeiro erro da aplicação.

## Decisão

**Release não aprovada — P0 pendente**
