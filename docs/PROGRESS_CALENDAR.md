# Calendário de progresso

A rota canônica é `GET /progress/calendar`; `GET /progress` redireciona preservando ano e mês. `GET /progress/calendar/data` oferece o mesmo modelo em JSON e `GET /progress/day/{yyyy-MM-dd}` mostra o detalhe. A consulta mensal carrega hábitos, agenda e conclusões em lote, sempre delimitados por `client_id` e `user_id`.

A grade começa no domingo, termina no sábado e inclui células externas com contraste reduzido. O mobile inicia em lista e permite alternar para calendário compacto.
