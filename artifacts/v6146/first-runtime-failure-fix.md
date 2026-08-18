# Primeira falha real e correção — v6.14.6

- Fase: pré-check.
- Erro: ambiente entregue não é Windows e não contém `pwsh`, `dotnet` ou `psql`.
- Stack trace: não houve processo ASP.NET Core; portanto não existe stack trace de runtime a registrar.
- Causa: validação foi iniciada fora do ambiente alvo `C:\MNSOFT\HabitFlow`.
- Arquivo alterado: `scripts/validation/run-release-candidate-local-windows.ps1`.
- Correção: runner promovido para v6.14.6, nomes de artifacts alinhados à release, bloqueio explícito fora do Windows e pré-check agregado com instruções de instalação/reexecução.
- Validação após correção: checks estáticos e npm no executor disponível; reexecução funcional permanece obrigatória no Windows real.
