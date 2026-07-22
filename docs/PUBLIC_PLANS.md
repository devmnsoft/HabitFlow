# Planos públicos

`GET /plans` é público para visitantes. A página mostra Gratuito, Premium Mensal e Premium Anual com CTAs comerciais.

Rotas sensíveis continuam protegidas por autenticação, incluindo `GET /billing` e `POST /billing/checkout`. Visitantes interessados no Premium são direcionados para cadastro com intenção do plano.
