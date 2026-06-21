# SUPORTE_WHATSAPP.md — Atendimento MNSOFT

## Dados públicos
- Empresa: MNSOFT
- Razão social: MNSOLUÇÕES TECNOLÓGICAS & CONSULTORIA LTDA
- CNPJ: 18.160.057/0001-13
- E-mail comercial/suporte: comercial@mnsoft.com.br

## Configuração
Admins gerais configuram atendimento em Admin Geral > Configurações de Atendimento. Os dados ficam em `systemSettings/public` e são alterados apenas pela Function `updateSystemSettings`.

## WhatsApp
O número aceita somente dígitos, `+`, espaços, parênteses e hífen, sendo normalizado para formato internacional, por exemplo `5591999999999`. O link é gerado como `https://wa.me/{numero}?text={mensagem}`.

## Onde aparece
Landing page, rodapé, contato, Perfil/Suporte e chatbot, apenas quando `whatsappEnabled=true` e o número é válido.

## Logs e segurança
Cliques geram `whatsapp_clicked` com origem. Usuário comum não altera WhatsApp. Campos são sanitizados e renderizados com escaping/textContent sempre que possível.
