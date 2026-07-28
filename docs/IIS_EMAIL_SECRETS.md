# Segredo de e-mail no IIS

Configure `Email__Smtp__Password=<NOVA_SENHA_DE_APP>` no ambiente da máquina, secret store da hospedagem ou arquivo local fora do Git protegido por ACL. Conceda ao identity do Application Pool somente as permissões necessárias. Nunca versione o valor no `web.config`, appsettings, scripts ou documentação.

Depois da troca, recicle o Application Pool, execute um envio controlado e confira logs sanitizados. A credencial anteriormente compartilhada deve ser revogada e produção deve receber uma nova senha de app com 2-Step Verification habilitada.
