# Auditoria inicial v6.9.2

- Data UTC: 2026-07-30T23:09:36Z
- HEAD inicial: `28c3da1561f5833df9b396c4a375f2c4f39bbbff`
- Branch base observada: `work` (checkout fornecido sem remote configurado)
- PR #80: merge `28c3da1` presente no HEAD.

## Ferramentas
```text
/bin/bash: line 1: dotnet: command not found
/bin/bash: line 1: psql: command not found
v24.15.0
npm warn Unknown env config "http-proxy". This will stop working in the next major version of npm.
11.4.2
```
/bin/bash: line 3: dotnet: command not found
/bin/bash: line 3: dotnet: command not found

- dotnet clean exit code: 127
- dotnet restore exit code: 127

### `dotnet build src/HabitFlow.Domain/HabitFlow.Domain.csproj --configuration Release --no-restore`
```text
/tmp/audit-builds.sh: line 9: dotnet: command not found
exit code: 127
```

### `dotnet build src/HabitFlow.Application/HabitFlow.Application.csproj --configuration Release --no-restore`
```text
/tmp/audit-builds.sh: line 9: dotnet: command not found
exit code: 127
```

### `dotnet build src/HabitFlow.Infrastructure/HabitFlow.Infrastructure.csproj --configuration Release --no-restore`
```text
/tmp/audit-builds.sh: line 9: dotnet: command not found
exit code: 127
```

### `dotnet build src/HabitFlow.Web/HabitFlow.Web.csproj --configuration Release --no-restore`
```text
/tmp/audit-builds.sh: line 9: dotnet: command not found
exit code: 127
```

### `test -f tests/HabitFlow.Tests/HabitFlow.Tests.csproj && dotnet build tests/HabitFlow.Tests/HabitFlow.Tests.csproj --configuration Release --no-restore`
```text
/tmp/audit-builds.sh: line 9: dotnet: command not found
exit code: 127
```

### `dotnet build HabitFlow.sln --configuration Release --no-restore`
```text
/tmp/audit-builds.sh: line 9: dotnet: command not found
exit code: 127
```
