# Primeiro Super Administrador

Não existe senha padrão nem SuperAdmin no seed. Em um terminal interativo, configure `ConnectionStrings__DefaultConnection` e execute:

```powershell
dotnet run --project src/HabitFlow.Web -- admin create-superadmin --email admin@example.com --name "Administrador"
```

A senha é lida duas vezes sem eco e nunca integra argumentos, logs ou SQL exibido. O comando cria/promove o usuário global (`client_id null`), atribui `super_admin`/`Platform.FullAccess`, incrementa `session_version` e audita tudo em uma transação. O wrapper `scripts/admin/create-superadmin.ps1` executa o mesmo fluxo.

Use `reset-superadmin-password` para redefinir e revogar sessões/tokens. Use `promote-superadmin` para um usuário existente; a confirmação literal `PROMOVER` é obrigatória.
