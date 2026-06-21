# Build de produção

- Desenvolvimento local: `npm start` ou `npm run dev`, ambos na porta 5177.
- Build: `npm run build` gera `dist/` minificado, sem source maps e com hardening pós-build.
- Preview: `npm run preview` publica `dist/` em `http://localhost:5177`.
- Deploy: `npm run deploy` executa build e `firebase deploy`.

O Firebase Hosting publica apenas `dist/`, evitando publicar os arquivos fonte originais de `assets/js`.
