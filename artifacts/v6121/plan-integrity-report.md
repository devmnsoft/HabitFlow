# Relatório de integridade comercial v6.12.1

Auditoria baseada nas migrations `033_plan_prices_features.sql` e `056_secure_admin_honest_plans_legal_privacy.sql` e nas mesmas regras executadas por `PlanIntegrityService`.

## Gratuito (`free`)

- **Mensal:** R$ 0,00
- **Anual:** R$ 0,00
- **Público:** Sim
- **Vendável:** Não (sem checkout pago)
- **Status de venda:** Available
- **Features implementadas:** Hábitos ativos; Objetivos ativos; Biblioteca completa; Resumo semanal; Exportação CSV; Impressão de relatórios; Histórico completo; Limite de histórico; Categorias personalizadas (somente quando o valor configurado as oferece)
- **Features bloqueadas:** Lembretes por hábito (Partial); Relatórios avançados (Partial); Rotinas compartilhadas (Partial); Objetivos compartilhados (Planned); Relatórios consolidados (Planned); Suporte prioritário (Planned); Pessoas da conta, Convites, Painel da conta e Comunicações internas (Internal)
- **Problemas encontrados:** copies genéricas podiam sugerir capacidades ausentes.
- **Correções aplicadas:** comparação agora deriva somente de features filtradas como `Implemented` e `is_marketable=true`, sem fallback “avançado”, “ampliado” ou “completo”.

## Ritmo (`ritmo`)

- **Mensal:** R$ 19,90
- **Anual:** R$ 199,00
- **Público:** Sim
- **Vendável:** Sim, condicionado a preço ativo e integridade das features
- **Status de venda:** Available
- **Features implementadas:** Hábitos ativos; Objetivos ativos; Biblioteca completa; Resumo semanal; Exportação CSV; Impressão de relatórios; Histórico completo; Categorias personalizadas
- **Features bloqueadas:** Lembretes por hábito (Partial); Relatórios avançados (Partial); Rotinas compartilhadas (Partial); Objetivos compartilhados (Planned); Relatórios consolidados (Planned); Suporte prioritário (Planned); Pessoas da conta, Convites, Painel da conta e Comunicações internas (Internal)
- **Problemas encontrados:** comparação usava `advanced_reports` e fallbacks comerciais mesmo quando a feature não era marketable.
- **Correções aplicadas:** benefícios e comparação usam somente features implementadas/marketable; mensal/anual e ciclo de cadastro/checkout são preservados; economia anual é calculada (aproximadamente 17%).

## Evolução (`evolucao`)

- **Mensal:** R$ 49,90
- **Anual:** R$ 499,00
- **Público:** Não
- **Vendável:** Não
- **Status de venda:** Grandfathered
- **Features implementadas:** não usadas em nova oferta comercial.
- **Features bloqueadas:** todas para novas contratações enquanto o plano permanecer Grandfathered.
- **Problemas encontrados:** nenhum na oferta pública, pois o repositório e o checkout bloqueiam o plano.
- **Correções aplicadas:** mantido fora do catálogo público e inelegível para checkout.

## Resultado

A oferta nova fica limitada a Gratuito e Ritmo. Recursos Partial, Planned, Internal, Deprecated ou não marketable são bloqueados de benefícios e checkout. A validação em runtime deve ser executada contra o banco do ambiente antes da publicação comercial.
