# HabitFlow v6.28.0 - Enterprise Governance

## Diagnostico inicial

- Baseline em `main`: `dotnet clean`, `restore`, `build`, `test`, `npm ci`, `npm test`, `npm audit --omit=dev` e `git diff --check` executados.
- Falha encontrada: `npm run security:scan` bloqueava um fixture de teste que contem uma senha simulada para validar sanitizacao de erros.
- Correcao aplicada: whitelist pontual em `scripts/security-scan.js` para o arquivo/regra especificos, mantendo a regra ativa para o restante do projeto.
- O projeto ja possuia base de MFA/TOTP, recovery code com hash, IA multi-provider desabilitada por padrao, legal documents, billing, suporte com SLA e catalogo Team/Enterprise.

## Implementacao

- SSO Enterprise: adicionada configuracao segura em `Authentication:Sso`, desabilitada por padrao, sem client secret versionado, com politica que exige entitlement, HTTPS, callback local e tokens assinados.
- MFA/governanca: adicionada politica de MFA por tenant para owners, admins e perfis sensiveis; SuperAdmin continua coberto pelo servico existente.
- White label: adicionada politica por tenant com nome comercial, logo/favicons controlados, cores, suporte e validacao de contraste para CTAs.
- Dominios: adicionados estados `NotConfigured`, `WaitingDns`, `Verified`, `Active`, `Error`, `Suspended`, bloqueio de duplicidade e ativacao sem verificacao.
- Permissoes: adicionadas permissoes backend para SSO/MFA, white label, dominios, integracoes, API keys, webhooks, IA e juridico.
- Billing/entitlements: adicionados codigos Enterprise para SSO, MFA obrigatorio, white label, dominio, API keys, auditoria avancada, SLA e governanca de IA.
- IA Enterprise: adicionada politica que bloqueia provider desabilitado, modelo fora de allow-list, dados sensiveis sem governanca e uso sem entitlement.
- Banco: criada migration incremental `102_v6280_enterprise_governance.sql` e sincronizado `database/migrate.sql` e `database/script_completo.sql`.

## Matriz de aceite

| Area | Cenario | Resultado esperado | Validacao |
|---|---|---|---|
| SSO | Provider desabilitado | Login local seguro e sem auto-login inseguro | Teste v6.28 |
| SSO | Configuracao invalida | Bloqueia ativacao por entitlement/HTTPS/callback/tokens | Teste v6.28 |
| MFA | Politica tenant | Perfis sensiveis podem ser exigidos por tenant | Migration + dominio |
| White Label | Cor ilegivel | Configuracao bloqueada | Teste v6.28 |
| Dominio | Duplicado ou invalido | Configuracao bloqueada | Teste v6.28 |
| Dominio | DNS nao verificado | Nao ativa dominio | Teste v6.28 |
| Permissoes | Admin outro tenant | Acesso negado pelo backend | Teste v6.28 |
| Billing | Sem entitlement | Recurso Enterprise falha fechado | Teste v6.28 |
| IA | Modelo fora da lista | Requisicao bloqueada | Teste v6.28 |
| Banco | Upgrade incremental | Tabelas e constraints Enterprise incluidas | Teste v6.28 |

## Pendencias reais para producao

- Configuracao real de provedores OIDC/OAuth2/SAML por tenant ainda depende de secrets em variaveis de ambiente e endpoints externos homologados.
- Verificacao DNS/SSL automatica nao foi implementada; o sistema apenas modela estados seguros e impede ativacao sem verificacao.
- API keys Enterprise permanecem marcadas como `Planned`, para nao vender promessa sem backend completo.
- Playwright visual completo e screenshots desktop/mobile devem ser executados em ambiente com browser e massa autenticada.
- As vulnerabilidades `npm audit` totais vistas no `npm ci` ficam em dependencias dev; `npm audit --omit=dev` passou limpo.

