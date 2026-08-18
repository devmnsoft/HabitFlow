# Validação do repository LGPD

`LgpdRepository.ListConsentsAsync` continua consultando diretamente `habitflow.user_privacy_consents`. Nenhum `try/catch`, retorno vazio ou fallback silencioso foi adicionado. A causa é tratada por migration de reparo e pelo gate explícito do runner.
