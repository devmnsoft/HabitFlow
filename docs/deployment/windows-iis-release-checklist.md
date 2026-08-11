# HabitFlow v6.12.0 — release Windows/IIS sem Docker

## 1. Requisitos e princípio operacional

- Windows Server 2022 ou mais recente, IIS com **Web Server**, **Static Content**, **WebSocket Protocol** e Management Tools.
- .NET 10 SDK para build e **ASP.NET Core Hosting Bundle 10** no servidor. Baixe somente de `https://dotnet.microsoft.com/download/dotnet/10.0`; depois da instalação execute `iisreset` em janela de manutenção.
- PostgreSQL suportado acessível pela rede e `psql` no host administrativo. Docker não é necessário.
- Node.js 24 LTS e npm apenas na estação/runner de validação; não são necessários no App Pool.
- DNS/TLS, backup testado e uma identidade de serviço sem login interativo.

## 2. Validação antes do pacote

Em PowerShell, na raiz do clone:

```powershell
.\scripts\windows\setup-dev.ps1
$env:HABITFLOW_DB_CONNECTION = '<connection string obtida do cofre>'
.\scripts\windows\validate-local.ps1
.\scripts\windows\publish-iis.ps1
```

O script não cria credenciais. Não grave a conexão no histórico, no repositório ou no pacote; prefira injeção pelo cofre/CI. O pacote fica em `artifacts/v6120/iis-publish` e contém um checklist curto para o operador.

## 3. App Pool e site

1. Crie um App Pool x64 dedicado, **No Managed Code**, Integrated e `Start Mode=AlwaysRunning`.
2. Use identidade dedicada, `Load User Profile=true`, limite de fila e recycling em janela controlada.
3. Aponte o site para uma pasta versionada (ex.: `C:\sites\HabitFlow\releases\v6.12.0`) e use binding HTTPS.
4. Conceda **Read & Execute** no release. Conceda **Modify** somente em pastas explícitas de logs/chaves; nunca na árvore inteira.
5. Instale o Hosting Bundle antes de criar o site ou execute Repair e reinicie IIS.

## 4. Configuração externa e segredos

Configure no ambiente do processo/IIS, nunca em arquivos commitados:

- `ASPNETCORE_ENVIRONMENT=Production`;
- `ASPNETCORE_URLS` somente quando a integração IIS exigir valor explícito;
- `ConnectionStrings__DefaultConnection` obtida de um cofre, com TLS e usuário de privilégio mínimo;
- chaves de provedores e SMTP por variáveis/cofre;
- diretório persistente de **Data Protection Keys**, fora do release, com ACL apenas para a identidade do App Pool e proteção DPAPI/certificado.

Não habilite stdout permanentemente. Durante diagnóstico, direcione-o para pasta com ACL restrita, limite retenção e desabilite após coletar o incidente. Centralize application/event logs e configure alerta para HTTP 5xx, falhas de banco e reinícios.

## 5. Banco, backup e migrations

1. Faça backup consistente e registre ponto de restauração antes da mudança.
2. Teste restauração em ambiente isolado.
3. Execute uma única vez o runner canônico (não combine com `script_completo.sql` no mesmo banco):

```powershell
.\scripts\windows\run-migrations.ps1 -ValidateRerun
```

4. Confirme `habitflow.schema_migrations`, checksums, versão mais recente e ausência de sessão bloqueada. O runner usa advisory lock.
5. Pare se houver divergência de checksum; não altere manualmente o registro.

## 6. Deploy, health check e pós-deploy

1. Coloque o site em drain/offline, copie o release para nova pasta e preserve a pasta externa de chaves.
2. Troque o caminho físico de forma atômica, inicie o App Pool e valide `GET /health` e logs.
3. Faça smoke de `/`, `/plans`, `/privacy`, `/help`, login e rotas essenciais autenticadas.
4. Rode Playwright contra o site publicado:

```powershell
$env:HABITFLOW_AUTH_STORAGE = (Resolve-Path 'playwright/.auth/user.json')
.\scripts\windows\run-playwright.ps1 -Suite all -BaseUrl 'https://habitflow.exemplo'
```

Para criar o estado local sem armazenar senha:

```powershell
.\scripts\windows\create-playwright-auth-state.ps1 -BaseUrl 'https://habitflow.exemplo'
```

O arquivo fica em `playwright/.auth/`, ignorado pelo Git. Não reutilize estado expirado nem o envie como artifact público.

## 7. Rollback

- Critérios: health sem sucesso, 5xx crítico, migration incompatível ou fluxo essencial quebrado.
- Reaponte o site para o release anterior e recicle o App Pool. Não reverta schema destrutivamente sem runbook aprovado.
- Quando a migration não for backward-compatible, restaure o backup em instância isolada, valide e faça o cutover conforme o plano de dados.
- Preserve logs, SHA, horários, versão do schema e evidências; abra análise de causa raiz antes de nova tentativa.
