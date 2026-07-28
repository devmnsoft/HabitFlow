# Outbox de e-mail transacional

`habitflow.transactional_email_outbox` separa commit de negócio e SMTP. O worker reivindica lotes com `FOR UPDATE SKIP LOCKED`, usa idempotency key, retry com backoff e encerra em `DeadLetter`. Uma falha não interrompe o lote. Ao enviar, remove o payload sensível; logs registram apenas o ID da mensagem e tipo de erro, nunca recipient, link ou token.
