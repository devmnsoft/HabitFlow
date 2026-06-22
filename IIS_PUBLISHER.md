# Publicador IIS do HabitFlow

O publicador IIS gera localmente uma pasta pronta para publicação no Windows/IIS, contendo o build estático, o `web.config` e um arquivo `README_PUBLICACAO_IIS.txt` com instruções rápidas.

## Comandos

```bash
npm run publish:iis
npm run publish:iis:nozip
npm run publish:iis:zip
```

- `publish:iis` respeita `scripts/publisher/publisher.config.json`, se existir.
- `publish:iis:nozip` nunca gera pacote binário; gera apenas `publish/iis/HabitFlow-IIS/`.
- `publish:iis:zip` gera um ZIP local em `publish/` para envio manual ao servidor.

## Configuração

Copie `scripts/publisher/publisher.config.example.json` para `scripts/publisher/publisher.config.json` e ajuste conforme o ambiente local.

```json
{
  "generateZip": false,
  "copyToIis": false,
  "iisPath": "C:\\inetpub\\wwwroot\\HabitFlow"
}
```

Por padrão, `generateZip` fica desativado para evitar binários no ambiente de desenvolvimento/Codex. Ative com `"generateZip": true` apenas no computador ou servidor do usuário quando quiser gerar o pacote compactado localmente.

## Atenção sobre GitHub e PRs

Os arquivos gerados em `publish/` e os pacotes `.zip` são artefatos locais de publicação. Eles não devem ser enviados ao GitHub. O repositório deve conter apenas o código-fonte, scripts, `web.config` e documentação.

A pasta `publish/`, a pasta `dist/`, pacotes compactados, source maps e binários são ignorados pelo Git.
