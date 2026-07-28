
## Password recovery / Gmail

- Aplicar migration 048 pelo runner canônico e confirmar tabelas de token, requests e outbox.
- Definir PublicBaseUrl HTTPS oficial e allowlist; localhost é proibido em produção.
- Confirmar que a senha SMTP não está no Git, revogar a senha compartilhada anteriormente e provisionar uma nova senha de app protegida no IIS.
- Manter 2-Step Verification habilitada; validar envio controlado por evidência sanitizada, sem recipient, payload ou token.
