# Fallback da Habit Library

Se o banco ainda não possuir as tabelas da biblioteca e o PostgreSQL retornar `42P01`, o serviço usa sugestões em memória com GUIDs fixos. Visitantes continuam explorando `/habit-library` e usuários logados podem adicionar templates fallback como hábitos reais.
