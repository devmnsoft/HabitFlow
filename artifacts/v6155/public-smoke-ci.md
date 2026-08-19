# Contrato do smoke público no CI

O job `runtime-smoke-public` baixa o publish, aplica migrations, aguarda o processo publicado por até 120 segundos e consulta `/`, `/login`, `/register`, `/plans` e `/favicon.ico`. HTTP 500 e padrões de exceção definidos no workflow reprovam o job.

O artifact de execução substitui este contrato com os códigos HTTP reais; nenhum resultado local é declarado aqui.
