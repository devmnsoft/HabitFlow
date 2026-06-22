# HabitFlow IIS Publisher Pro

O Publicador IIS gera localmente uma pasta pronta para copiar para o IIS, sem versionar `publish/`, `dist/`, ZIPs, source maps ou binários. O Firebase Hosting continua preservado e usa o fluxo `firebase deploy`.

## Comandos

```bash
npm run publish:iis:nozip
npm run publish:iis:zip
npm run publish:iis:check
```

- `publish:iis:nozip`: build + pacote em `publish/iis/HabitFlow-IIS`, sem ZIP.
- `publish:iis:zip`: gera ZIP local opcional em `publish/` e avisa para não versionar.
- `publish:iis:check`: gera/valida pacote sem ZIP e sem cópia para IIS.

## Windows um clique

Execute `scripts\publisher\publish-iis.bat`. Ele roda `npm run publish:iis:nozip` a partir da raiz e pausa no final.

## PowerShell

```powershell
.\scripts\publisher\publish-iis.ps1 -NoZip -Open
.\scripts\publisher\publish-iis.ps1 -Zip
.\scripts\publisher\publish-iis.ps1 -NoZip -CopyToIis
```

## Configuração local

Copie `scripts/publisher/publisher.config.example.json` para `scripts/publisher/publisher.config.json` e ajuste caminhos locais. O arquivo local fica no `.gitignore` para não expor ambiente do servidor.

## Segurança

O pacote bloqueia a publicação se encontrar `.env`, `node_modules`, `functions`, `.git`, `.github`, source maps, chaves privadas, tokens, `package.json`, `firebase.json`, `firestore.rules` ou `scripts/` dentro do pacote IIS.

## IIS e erro 500.19

O `web.config` usa IIS URL Rewrite para fallback SPA e bloqueios. Se o IIS retornar 500.19, instale o **IIS URL Rewrite Module**, habilite **Static Content** e revise MIME types duplicados.

## Rollback

Mantenha a pasta anterior do site antes da cópia. Para rollback, restaure o conteúdo anterior em `C:\inetpub\wwwroot\habitflow` e recicle o Application Pool/site se necessário.
