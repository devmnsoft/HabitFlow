# HabitFlow v6.16.8 — auditoria e estratégia PWA

## Diagnóstico anterior

O manifest possuía apenas um ícone SVG genérico e não declarava atalhos, categorias nem os tamanhos instaláveis. O service worker mantinha um cache público pequeno, mas usava um HTML estático diferente da rota solicitada e não tratava push, background sync ou atualização observável. A aplicação registrava o worker, porém não oferecia preferências/dispositivos push. As meta tags mobile da Apple e padrão estavam ausentes.

## Implementação

- Manifest instalável com identidade HabitFlow, três ícones (incluindo maskable), atalhos e metadados completos.
- Cache `habitflow-public-v6.16.8`, limitado a shell público estático. Navegações são sempre network-first com `no-store`; conta, autenticação, cobrança, exportação e administração nunca são interceptadas.
- `/offline` é precacheada e não contém dados pessoais. A última sincronização exibida vem somente de timestamp local.
- A fila IndexedDB guarda apenas ID idempotente, URL/ação mínima e expira em 24 horas. Respostas 401 pausam para novo login, 409 é tratado como convergência idempotente e erros 4xx definitivos removem o evento.
- Web Push usa VAPID configurado somente por configuração/variáveis de ambiente. A chave privada não é enviada ao browser nem registrada em logs. Subscriptions e preferências são sempre filtradas por `client_id` e `user_id` autenticados.
- O runtime primeiro persiste a notificação interna e conclui o dispatch; depois tenta push de forma complementar. Endpoints expirados são desativados e falha do provider não derruba o worker.
- O limite diário é backend e atômico por lembrete lógico, usuário, canal e dia local da preferência. Vários dispositivos não multiplicam consumo. Falha total do provider libera a reserva; aceitação por pelo menos um dispositivo consome uma unidade.
- Estados de push usam linguagem honesta: `Processing`, `ProviderAccepted`, `Failed`, `SubscriptionInvalid` ou `Suppressed`. `ProviderAccepted` significa que o provider aceitou a requisição; não é prova de exibição no dispositivo.
- Assinaturas são validadas contra destinos locais, privados, link-local, metadata e hosts não resolvíveis antes de salvar e antes de enviar. HTTPS isolado não é tratado como suficiente.
- Idiomas aceitos nas preferências: `pt-BR`, `en-US`, `es-ES` e `fr-FR`. Idioma e timezone continuam campos separados.

## Operação

Configure `PushNotifications__Enabled=true`, `PushNotifications__Subject`, `PushNotifications__PublicKey` e `PushNotifications__PrivateKey` no secret store do ambiente. Nunca grave a chave privada no repositório. A tela `/notifications/preferences` pede permissão somente após gesto explícito.

## Evidências e limitações do ambiente

A evidência visual deve ser gerada pelo job Playwright com os sete viewports. O container de implementação não possui o runtime .NET nem browsers Playwright instalados; por isso build, execução do servidor, screenshot, Lighthouse e migrations PostgreSQL precisam ser executados no CI preparado para a solução. Os testes Node, scanner de segurança, audit e verificação de whitespace foram executados localmente.
