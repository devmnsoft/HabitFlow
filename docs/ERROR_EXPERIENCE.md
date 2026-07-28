# Experiência de erro

APIs respondem `application/problem+json; charset=utf-8`; HTML responde `text/html; charset=utf-8` com mensagem humana, código de atendimento, nova tentativa, início e ajuda. A resposta pública nunca inclui stack, SQL, connection string, constraint, payload ou token. Logs estruturados incluem correlation id, fingerprint, rota, método, ids mascarados, tipo e duração. Detalhes de desenvolvimento permanecem nos logs locais.
