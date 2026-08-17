# Smoke autenticado — v6.13.7

**Status: não executado.** Startup, PostgreSQL e PowerShell estão indisponíveis; portanto login, sessão, renderização, reload e logs das rotas autenticadas não foram validados.

O helper existente foi corrigido para: gravar neste artifact; incluir favoritos, criação de objetivo e revisão semanal; aceitar IDs opcionais de hábito/template/objetivo; repetir cada GET; detectar retorno ao login e conteúdo de exceção técnica. Senhas continuam recebidas como `SecureString`, nunca gravadas no repositório.

Todas as rotas autenticadas e rotas parametrizadas solicitadas permanecem P0 até execução real.
