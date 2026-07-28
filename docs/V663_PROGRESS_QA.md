# QA de progresso v6.6.3

- Executar `scripts/qa/clean-generated-output.ps1`; somente bin, obj, TestResults e artifacts são removidos.
- Executar `scripts/qa/check-progress-compile-contract.ps1` e o build real; o script não substitui compilação.
- Confirmar que nenhum `obj` está rastreado e nenhum gerado foi alterado manualmente.
- Compilar Domain, Application, Infrastructure, Web e solução; publicar Web Release para Razor.
- Aplicar apenas `database/migrate.sql` a banco descartável terminado em `_tests` e executar testes.
- Executar Playwright e guardar screenshots somente como artifacts.
- Confirmar ausência de secrets, binários, publish e screenshots no commit.
