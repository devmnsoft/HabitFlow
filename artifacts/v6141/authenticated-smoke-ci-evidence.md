# Smoke autenticado no CI

Foi confirmada uma lacuna real: o workflow existente não executava o script de smoke autenticado. O próprio gate foi ampliado (sem workflow paralelo) com `runtime-smoke-authenticated`, dependente de build/publish e migrations, PostgreSQL efêmero, download do publish, migrations, startup, usuário tenant-bound, seed, resolução de IDs, smoke e artifact.

O script agora aceita caminho de relatório e senha por `SecureString` ou variável de ambiente, e falha de forma explícita sem credencial em execução não interativa. O provisionador deixou de exibir senhas geradas.

Resultado remoto: pendente; nenhuma aprovação foi declarada sem run.
