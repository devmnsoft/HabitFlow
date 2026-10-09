using System.Globalization;
using HabitFlow.Domain;
using HabitFlow.Web.Models;

namespace HabitFlow.Web.Services;

public sealed class PlanLandingPageService(IPlanCatalogRepository repository)
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public async Task<PlanLandingPageViewModel> BuildAsync(CancellationToken ct)
    {
        var catalog = await repository.GetPublicCatalogAsync(ct);
        var cards = catalog.Select(ToCard).ToArray();
        if (cards.Length == 0) return BuildFallback();
        return new(cards,
        [
            new("Clareza todos os dias", "Veja quais hábitos importam hoje e o que pode esperar.", "calendar"),
            new("Progresso visível", "Acompanhe consistência e evolução semanal sem planilhas.", "chart"),
            new("Menos abandono", "Ajuste a rotina e retome sem transformar uma pausa em culpa.", "refresh"),
            new("Rotina personalizada", "Adapte dias, horários e objetivos ao seu momento.", "sliders"),
            new("Relatórios úteis", "Entenda o que funcionou e prepare a próxima semana.", "report"),
            new("Segurança e privacidade", "Controle dados, consentimentos e solicitações de privacidade.", "shield")
        ], BuildComparison(catalog), BuildFaq(),
        [
            new("Privacidade por padrão", "Política clara e Central de Privacidade para controlar seus dados.", "shield"),
            new("Conta sob seu controle", "Sessões gerenciáveis e proteção reforçada para a administração.", "lock"),
            new("15 dias grátis", "Experimente todos os recursos Premium sem cobrança inicial.", "card"),
            new("Dados preservados", "Ao cancelar ou alterar o plano, seus dados e hábitos nunca são apagados.", "check")
        ], new("Comece pequeno. Evolua no seu ritmo.", "Crie sua conta e descubra uma rotina consistente com 15 dias grátis.", "Começar 15 dias grátis", "/register"));
    }

    public static PlanLandingPageViewModel BuildFallback() => new(
        [new(PlanCodes.Ritmo, "Premium", "Para transformar intenção em consistência", "Experimente por 15 dias grátis todos os recursos de hábitos, metas e coach inteligente.", "15 dias grátis", true, "R$ 29,90/mês", "R$ 299,00/ano", "Economize cerca de 17%",
            ["15 dias de teste grátis", "Hábitos e objetivos ilimitados", "Coach IA assistido", "Relatórios e exportações", "Privacidade sob seu controle"], "Começar 15 dias grátis", "/register", true)],
        [new("Mais clareza no dia", "Organize os próximos passos sem transformar sua rotina em pressão.", "calendar"),
         new("Evolução sem perder dados", "Seus dados não são apagados quando sua assinatura muda.", "shield")],
        [new("Avaliação", "15 dias grátis com recursos completos", "Mais limites e governança de time sob medida"),
         new("Privacidade", "Central de Privacidade incluída", "Central de Privacidade incluída")], BuildFaq(),
        [new("Privacidade por padrão", "Você controla seus dados pela Central de Privacidade.", "shield")],
        new("Comece com 15 dias grátis", "Os detalhes dos planos estão sendo atualizados. O teste de 15 dias grátis continua disponível para novos cadastros.", "Começar 15 dias grátis", "/register"));

    private static CommercialPlanCardViewModel ToCard(PublicPlan plan)
    {
        var monthly = plan.Prices.FirstOrDefault(x => x.BillingCycle.Equals("Monthly", StringComparison.OrdinalIgnoreCase));
        var yearly = plan.Prices.FirstOrDefault(x => x.BillingCycle.Equals("Yearly", StringComparison.OrdinalIgnoreCase));
        var saving = monthly is not null && yearly is not null && yearly.Amount < monthly.Amount * 12
            ? $"Economize cerca de {(1 - yearly.Amount / (monthly.Amount * 12)):P0}" : null;
        var free = plan.Code.Equals(PlanCodes.Free, StringComparison.OrdinalIgnoreCase);
        var benefits = plan.Features.Take(7).Select(DescribeFeature).Where(x => x is not null).Cast<string>().ToArray();
        var name = plan.Code.Equals(PlanCodes.Ritmo, StringComparison.OrdinalIgnoreCase) ? "Premium"
            : plan.Code.Equals(PlanCodes.Free, StringComparison.OrdinalIgnoreCase) ? "Free"
            : plan.PublicName;
        return new(plan.Code, name, plan.AudienceText ?? (free ? "Para começar com o essencial" : "Para transformar intenção em consistência"),
            plan.Description ?? plan.Headline ?? "Uma rotina mais clara, no seu ritmo.", plan.BadgeText ?? (!free ? "Mais recomendado" : null),
            plan.IsFeatured || plan.Code.Equals(PlanCodes.Ritmo, StringComparison.OrdinalIgnoreCase),
            monthly is null ? null : monthly.Amount.ToString("C", PtBr) + "/mês",
            yearly is null ? null : yearly.Amount.ToString("C", PtBr) + "/ano", saving, benefits,
            free ? "Começar grátis" : $"Assinar {name}", free ? "/register" : $"/register?intent={Uri.EscapeDataString(plan.Code)}&cycle=Monthly",
            free || monthly is not null || yearly is not null);
    }

    public static string FormatLimit(int? value) => value is null ? "Não informado" : value < 0 ? "Ilimitado" : value.Value.ToString(PtBr);

    private static string? DescribeFeature(PlanFeatureValue feature) => feature.ValueType.ToLowerInvariant() switch
    {
        "boolean" when feature.BoolValue == true => feature.Name,
        "integer" when feature.IntValue is not null => $"{feature.Name}: {FormatLimit(feature.IntValue)}",
        "string" when !string.IsNullOrWhiteSpace(feature.StringValue) => $"{feature.Name}: {feature.StringValue}",
        _ => null
    };

    private static IReadOnlyList<PlanComparisonRowViewModel> BuildComparison(IReadOnlyList<PublicPlan> plans)
    {
        string Value(string code, string feature, string unavailable, Func<PlanFeatureValue, string>? format = null) {
            var value = plans.FirstOrDefault(p => p.Code.Equals(code, StringComparison.OrdinalIgnoreCase))?.Features.FirstOrDefault(f => f.Code == feature);
            if (value is null) return unavailable;
            if (format is not null) return format(value);
            if (value.IntValue is not null) return FormatLimit(value.IntValue);
            return value.BoolValue == true ? "Incluído" : value.StringValue ?? unavailable;
        }
        string Ai(string code)
        {
            var value = plans.FirstOrDefault(p => p.Code.Equals(code, StringComparison.OrdinalIgnoreCase))?.Features.FirstOrDefault(f => f.Code == PlanFeatureCodes.AiAssistant);
            return value?.BoolValue == true ? "Incluído" : "Não incluído";
        }
        return [
            new("Usuários na equipe", "1", "1", null, "Até 10", "Ilimitado"),
            new("Hábitos ativos", Value(PlanCodes.Free, PlanFeatureCodes.ActiveHabitsLimit, "Não informado"), Value(PlanCodes.Ritmo, PlanFeatureCodes.ActiveHabitsLimit, "Não informado"), null, "Ilimitado", "Ilimitado"),
            new("Objetivos ativos", Value(PlanCodes.Free, PlanFeatureCodes.ActiveGoalsLimit, "Não informado"), Value(PlanCodes.Ritmo, PlanFeatureCodes.ActiveGoalsLimit, "Não informado"), null, "Ilimitado", "Ilimitado"),
            new("Histórico", Value(PlanCodes.Free, PlanFeatureCodes.HistoryDaysLimit, "Não informado", x => x.IntValue is null ? "Não informado" : x.IntValue < 0 ? "Ilimitado" : $"{x.IntValue} dias"), Value(PlanCodes.Ritmo, PlanFeatureCodes.FullHistory, "Não incluído", _ => "Histórico completo"), null, "Completo", "Completo"),
            new("Biblioteca", Value(PlanCodes.Free, PlanFeatureCodes.FullHabitLibrary, "Não incluída"), Value(PlanCodes.Ritmo, PlanFeatureCodes.FullHabitLibrary, "Não incluída"), null, "Incluída", "Incluída"),
            new("Desafios", Value(PlanCodes.Free, "challenge_7_days", "7 dias", _ => "7 dias"), Value(PlanCodes.Ritmo, "challenge_90_days", "7, 30 e 90 dias", _ => "7, 30 e 90 dias"), "O progresso considera uma conclusão por dia, a partir do início do desafio.", "7, 30 e 90 dias", "7, 30 e 90 dias"),
            new("Relatórios", Value(PlanCodes.Free, PlanFeatureCodes.BasicReports, "Não incluídos", _ => "Resumo semanal básico"), Value(PlanCodes.Ritmo, PlanFeatureCodes.BasicReports, "Não incluídos", _ => "Relatórios disponíveis implementados"), null, "Avançados", "Avançados e consolidados"),
            new("Exportação", Value(PlanCodes.Free, PlanFeatureCodes.ReportExportCsv, "Não incluída", _ => "Exportação CSV"), Value(PlanCodes.Ritmo, PlanFeatureCodes.ReportExportCsv, "Não incluída", _ => "Exportação CSV"), null, "CSV", "CSV"),
            new("Lembretes por hábito", "Conforme catálogo", "Conforme catálogo", null, "Ampliados", "Ilimitados"),
            new("Metas compartilhadas / times", "—", "—", null, "Incluído", "Incluído"),
            new("PWA", "Incluído", "Incluído", null, "Incluído", "Incluído"), new("Push notifications", "Em breve", "Em breve", null, "Em breve", "Em breve"),
            new("Assistente", Ai(PlanCodes.Free), Ai(PlanCodes.Ritmo), "O assistente usa só totais da própria conta e não libera recurso pago.", Ai(PlanCodes.Team), Ai(PlanCodes.Enterprise)), new("Conquistas e metas semanais", "Em breve", "Em breve", null, "Incluído", "Incluído"),
            new("Suporte", "Incluído", "Incluído", null, "Prioritário", "Dedicado"), new("Exportação PDF", "Em breve", "Em breve", null, "Em breve", "Incluída"),
            new("Segurança da conta", "Incluída", "Incluída", null, "Incluída", "Incluída"), new("Central de Privacidade", "Incluída", "Incluída", null, "Incluída", "Incluída")];
    }

    private static IReadOnlyList<PlanFaqItemViewModel> BuildFaq() => [
        new("Posso começar grátis?", "Sim! Novos usuários começam com 15 dias grátis de teste com acesso completo a hábitos, metas e coach inteligente, sem necessidade de cartão de crédito antecipado."),
        new("O que muda no Premium?", "O Premium amplia limites e recursos implementados que aparecem na comparação. Mensal e anual têm os mesmos recursos."),
        new("Meus hábitos somem se eu cancelar?", "Não. Seus hábitos e dados continuam preservados; após o período ou cancelamento, novos hábitos ficam restritos até a reativação de um plano."),
        new("Posso usar no celular?", "Sim. A interface é responsiva e o PWA pode ser instalado quando o navegador e o dispositivo oferecem suporte."),
        new("Relatórios e exportações estão inclusos?", "O resumo e a exportação CSV aparecem conforme o catálogo. PDF e recursos parciais não são vendidos como disponíveis."),
        new("Como funcionam desafios?", "Os desafios disponíveis registram uma conclusão por dia desde o início. As durações liberadas constam na comparação."),
        new("Posso cancelar depois?", "Sim. O plano contratado permanece ativo até o fim do período já pago quando aplicável. Cancelar ou fazer downgrade nunca apaga seus dados."),
        new("O pagamento já está ativo?", "O checkout usa a integração real com Mercado Pago quando configurada. A ativação só ocorre após confirmação segura pelo webhook; se o ambiente não estiver configurado, mostramos uma mensagem e o suporte."),
        new("Tem plano para equipes ou empresas?", "Sim. O Team libera até 10 usuários e recursos de colaboração; para volume maior e condições sob medida existe o Enterprise."),
        new("Como assinar o Enterprise?", "O Enterprise é vendido por contato, sem checkout online. Fale com o comercial em comercial@mnsoft.com.br (CNPJ 18.160.057/0001-13).")];
}
