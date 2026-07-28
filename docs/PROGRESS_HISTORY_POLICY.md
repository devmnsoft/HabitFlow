# Política de histórico

A conta é resolvida por `client_id`. O plano efetivo e suas features são a fonte oficial; `users.plan` não participa da autorização. `full_history` indica capacidade geral e `history_days_limit` parametriza a janela (`90` no Gratuito; `-1` no Ritmo/Evolução significa sem limite funcional). A navegação e o acesso direto a mês/dia aplicam a mesma janela. Dados antigos permanecem armazenados; HTML explica a restrição e JSON devolve ProblemDetails 403 com código funcional.
