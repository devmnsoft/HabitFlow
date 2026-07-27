# PWA em produção

O cache contém somente a experiência pública/offline e ativos estáticos explicitamente permitidos. Dashboard, hábitos, objetivos, progresso, relatórios, conta, perfil, notificações, cobrança e SuperAdmin nunca são interceptados nem armazenados; respostas JSON também são excluídas. Uma nova versão aguarda confirmação da página antes de `skipWaiting`, evitando troca inesperada durante uma ação. O logout solicita limpeza dos caches criados pelo HabitFlow.
