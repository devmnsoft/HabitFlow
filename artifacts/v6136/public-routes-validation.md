# Rotas públicas — v6.13.6

> Data: 2026-08-17 UTC
> SHA inicial: `ee66750e8bac3a3df199e33df49af6ae5a3f958c`

## Evidência estática
Controllers declaram `/login`, `/register` e `/plans`; os ativos de service worker e favicon estão presentes na árvore web.

## Runtime
**Pendente.** A aplicação não pôde subir em `localhost:5097` porque o SDK/runtime .NET não está instalado. Portanto, códigos HTTP, redirects, middleware, DI e logs não foram declarados validados.
