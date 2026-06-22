# IIS Deploy HabitFlow v2.4 PRD

## Pré-requisitos
- Windows Server com IIS instalado.
- Role service **Static Content** habilitado.
- IIS URL Rewrite instalado. Sem ele, o `web.config` pode gerar erro 500.19.
- Compressão estática/dinâmica opcional habilitada.
- Certificado SSL para produção.

## Build
```bash
npm run build
```

## Publicação
1. Copie o conteúdo de `dist/` para `C:\inetpub\wwwroot\habitflow`.
2. Copie `web.config` para a mesma pasta publicada.
3. Crie/configure o site no IIS apontando para `C:\inetpub\wwwroot\habitflow`.
4. Configure binding HTTP/HTTPS e DNS do domínio.
5. Para forçar HTTPS, edite `web.config` e altere a regra `Force HTTPS` para `enabled="true"` após validar certificado.

## Arquitetura
O IIS hospeda apenas o frontend estático. Firebase Auth, Firestore e Firebase Functions continuam no Firebase do projeto `habitflow-5f945`. O domínio IIS deve estar autorizado em Firebase Auth Authorized Domains, App Check, CSP/connect-src e `APP_ALLOWED_ORIGINS` se algum endpoint HTTP for usado.

## Testes
- Abra a home e faça reload em `/dashboard` para validar fallback SPA.
- Teste login, hábitos, suporte, chatbot, PWA e console sem CORS.
- Limpe cache do navegador ou use Admin Geral > Diagnóstico > Limpar cache PWA.

## Troubleshooting
- **500.19**: instale IIS URL Rewrite ou revise XML/MIME duplicados.
- **MIME type**: confirme Static Content e MIME types do `web.config`.
- **Rotas 404**: confirme regra SPA fallback e URL Rewrite.
- **Functions falham**: valide Firebase Auth domains, App Check e Functions no Firebase; IIS não executa Functions.

## Rollback
Mantenha uma cópia da pasta anterior e restaure o conteúdo para `C:\inetpub\wwwroot\habitflow`, reciclando o site/app pool em seguida.
