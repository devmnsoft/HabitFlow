# QA visual v6.5.1

## Automação

A suíte `tests/HabitFlow.Playwright` visita Home, Demo, Biblioteca, Planos, Ajuda, Login e Cadastro como visitante, coleta console/page errors e falhas de rede, mede overflow, verifica header/main/footer e usa bounding boxes para detectar interseções. Screenshots e traces são artifacts; nunca entram no Git.

Personas autenticadas (Gratuito, proprietário e Super Administrador) exigem credenciais efêmeras fornecidas pela CI. A matriz alvo inclui Dashboard, Hábitos, Objetivos, Progresso, Relatórios, Perfil, Meu plano, Suporte e todas as rotas operacionais. Não há credenciais no repositório.

## Resultado local

A inspeção estática confirmou shell, landmarks, footer contextual, ordem de assets e ausência de layout explícito. A execução de navegador e servidor ficou **pendente por limitação do ambiente**, que não fornece `dotnet` nem permitiu baixar Playwright (HTTP 403). CI deve ser a fonte do resultado visual, console, rede e screenshots antes do merge.

## Limpeza para QA

Em janela anônima, DevTools → Application → Service Workers → Unregister; depois Clear site data e recarregamento forçado. Validar que `habitflow-public-v651-shell-1` substituiu caches anteriores.
