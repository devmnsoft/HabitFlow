# Runtime da Central de Privacidade

**Pendente de execução em Windows com banco e usuário autenticado.** Este contêiner não possui SDK .NET, PowerShell, PostgreSQL nem credenciais locais. Não se declara que `/account/privacy` abriu ou persistiu preferências sem essa execução.

A correção elimina a causa de schema no stream de migrations; o aceite runtime ainda requer abrir a rota autenticada, alternar `analytics`/`communications`, recarregar e inspecionar os logs contra `42P01`/`PostgresException`.
