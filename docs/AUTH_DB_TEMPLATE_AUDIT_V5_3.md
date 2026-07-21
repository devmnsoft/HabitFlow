# Auditoria v5.3 Auth UX, Database Messages e Template Contextual

## Problemas encontrados
- Cadastro não tinha confirmação de senha no contrato `RegisterDto`.
- Login e cadastro tinham campos de senha sem componente padronizado com olhinho.
- Mensagens globais usavam alertas Bootstrap simples e podiam se confundir com o fundo.
- Erros PostgreSQL misturavam mensagem técnica e mensagem pública.
- Não havia painel visual dedicado para diagnóstico de banco em Development/Admin.

## Arquivos alterados
- DTO, serviço de autenticação, helper PostgreSQL e controller de autenticação.
- Layout, partials de mensagens, partial de senha, views de login/cadastro/help/diagnóstico.
- CSS e JavaScript globais.
- Documentação v5.3.

## Validações aplicadas
- Senha obrigatória, mínimo 8 caracteres e confirmação igual antes de acessar o banco.
- Mensagens DatabaseError separadas de ValidationError/Error.
- Senhas não são registradas em auditoria nem metadata.
- SVG inline contextual sem binários.

## Pendências
- Validar fluxo com PostgreSQL real quando o SDK .NET e o serviço de banco estiverem disponíveis no ambiente.
