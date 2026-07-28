# Arquitetura de layout v6.5.1

`Views/Shared/_Layout.cshtml` é o único documento HTML completo. `_ViewStart` seleciona apenas `_Layout`; os quatro layouts intermediários foram removidos. `LayoutContextResolver` recebe `HttpContext`, `RouteData` e `ViewDataDictionary`, aceita somente `ViewData["NavigationContext"]` como override e resolve Public, Personal, Account ou Platform.

O shell produz header, mensagens, conteúdo, footer, hosts globais e scripts uma vez. Public/Personal usam largura de 1180 px. Account/Platform usam 1440 px, sidebar sticky no desktop e offcanvas Bootstrap no mobile. A Plataforma nunca renderiza o footer institucional. O menu da pessoa concentra plano, conta, preferências, instalação, acesso operacional condicional e saída.
