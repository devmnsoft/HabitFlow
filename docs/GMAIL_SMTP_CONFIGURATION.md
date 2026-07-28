# Gmail SMTP

A configuração pública usa `smtp.gmail.com:587`, STARTTLS obrigatório, remetente `HabitFlow <comercial@mnsoft.com.br>` e Reply-To equivalente. A senha SMTP não fica no Git. Configure somente por user-secrets ou variável protegida:

```sh
dotnet user-secrets set "Email:Smtp:Password" "<NOVA_SENHA_DE_APP>"
```

A senha de app compartilhada anteriormente deve ser revogada. Produção requer uma nova senha de app e a verificação em duas etapas (2-Step Verification) deve permanecer habilitada. Testes automatizados não enviam pelo Gmail.
