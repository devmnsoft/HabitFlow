# Smoke público — v6.13.8

O job `runtime-smoke-public` inicializa o publish em `127.0.0.1:5097` com PostgreSQL migrado e consulta `/`, `/login`, `/register`, `/plans`, `/service-worker.js` e `/favicon.ico`. HTTP 5xx e assinaturas de exceção no log falham o job.

Status: **pendente de execução externa**; o artifact do workflow conterá os HTTP reais e o log de startup.
