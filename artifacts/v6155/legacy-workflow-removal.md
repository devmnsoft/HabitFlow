# Remoção do workflow legado

`.github/workflows/v6138-release-gate.yml` foi removido. Ele combinava nomenclaturas e artifacts históricos, podia concorrer em pull requests e já havia sido substituído por `.github/workflows/habitflow-dotnet-release-gate.yml`.

O conteúdo não foi migrado e nenhum gate secundário foi criado.
