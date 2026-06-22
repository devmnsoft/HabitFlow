# Publicação IIS

Use o publicador para gerar uma saída local compatível com IIS:

```bash
npm run publish:iis:nozip
```

A saída será criada em `publish/iis/HabitFlow-IIS/`. Copie o conteúdo dessa pasta para o diretório do site no IIS.

Para gerar um ZIP local opcional:

```bash
npm run publish:iis:zip
```

## Atenção

Os arquivos gerados em `publish/` e os pacotes `.zip` são artefatos locais de publicação. Eles não devem ser enviados ao GitHub. O repositório deve conter apenas o código-fonte, scripts, `web.config` e documentação.
