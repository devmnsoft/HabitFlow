# QA PostgreSQL v6.8.1

Use somente banco cujo nome termine em `_tests`. Aplique `scripts/database/run-migrations.sh` duas vezes antes dos testes. O ambiente desta entrega não possui `psql` nem `dotnet`; migrations, concorrência, rollback e autenticação PostgreSQL permanecem para CI e não são declarados aprovados localmente.
