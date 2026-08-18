# Atualização do runner Windows

O runner foi promovido para v6.14.4 e grava em `artifacts/v6144`. Após migrations, ele:

- consulta os tipos reais das colunas relevantes em `information_schema`;
- executa `EXPLAIN` da projeção com o join de favoritos usando `ON_ERROR_STOP=1`;
- persiste somente tipos e plano, nunca connection string, senha ou IDs;
- mantém build/publish, startup, smoke público e autenticado;
- inclui biblioteca, favoritos, detalhe e customização no smoke existente;
- continua explicitando a jornada mutacional e regras Free como manuais até execução real.
