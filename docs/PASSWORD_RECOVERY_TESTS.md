# Testes da recuperação

A suíte deve cobrir geração/entropia/hash, validade/expiração/revogação/uso único, política de senha, resposta genérica, rate limit, templates HTML/texto e incremento de sessão. Integração PostgreSQL deve usar banco terminado em `_tests` e o runner canônico. Testes funcionais e Playwright cobrem seis rotas, antiforgery, login antigo/novo, sessão revogada, mobile, foco, contraste e overflow. SMTP real é somente manual, auditado e com secret efêmero.
