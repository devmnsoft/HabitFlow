# Correções de falhas do CI

Nenhuma falha de produto foi observada em logs remotos porque o workflow não pôde ser disparado. Consequentemente, nenhum código, validação ou regra de produto foi alterado e nenhum `continue-on-error` foi adicionado.

| Erro | Causa | Arquivo alterado | Correção | Run que falhou | Run confirmatório |
|---|---|---|---|---|---|
| Dispatch indisponível | GitHub CLI sem autenticação, repositório sem remote e API bloqueada pelo proxy | Somente evidências v6.14.2 | Registrar bloqueio com transparência | Não existe | Não existe |
