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
            new("Sem cartão no gratuito", "Experimente o fluxo essencial antes de decidir assinar.", "card"),
            new("Cancelamento simples", "Ao cancelar, seus dados não são apagados automaticamente.", "check")
        ], new("Comece pequeno. Evolua no seu ritmo.", "Crie sua conta grátis e descubra uma rotina mais clara, sem cartão.", "Começar grátis", "/register"));
    }

    public static PlanLandingPageViewModel BuildFallback() => new(
        [new(PlanCodes.Free, "Gratuito", "Para começar com o essencial", "Organize sua rotina e acompanhe o que importa hoje.", null, false, null, null, null,
            ["Comece sem cartão", "Hábitos e objetivos em um só lugar", "Privacidade sob seu controle"], "Começar grátis", "/register", true)],
        [new("Mais clareza no dia", "Organize os próximos passos sem transformar sua rotina em pressão.", "calendar"),
         new("Evolução sem perder dados", "Seus dados não são apagados quando sua assinatura muda.", "shield")],
        [new("Começar", "Hábitos e objetivos essenciais", "Mais limites e recursos quando disponíveis"),
         new("Privacidade", "Central de Privacidade incluída", "Central de Privacidade incluída")], BuildFaq(),
        [new("Privacidade por padrão", "Você controla seus dados pela Central de Privacidade.", "shield")],
        new("Comece gratuitamente", "Os detalhes das assinaturas estão sendo atualizados. O plano gratuito continua disponível.", "Começar grátis", "/register"));

    private static CommercialPlanCardViewModel ToCard(PublicPlan plan)
    {
        var monthly = plan.Prices.FirstOrDefault(x => x.BillingCycle.Equals("Monthly", StringComparison.OrdinalIgnoreCase));
        var yearly = plan.Prices.FirstOrDefault(x => x.BillingCycle.Equals("Yearly", StringComparison.OrdinalIgnoreCase));
        var saving = monthly is not null && yearly is not null && yearly.Amount < monthly.Amount * 12
            ? $"Economize cerca de {(1 - yearly.Amount / (monthly.Amount * 12)):P0}" : null;
        var free = plan.Code.Equals(PlanCodes.Free, StringComparison.OrdinalIgnoreCase);
        var benefits = plan.Features.Take(7).Select(DescribeFeature).Where(x => x is not null).Cast<string>().ToArray();
        return new(plan.Code, plan.PublicName, plan.AudienceText ?? (free ? "Para começar com o essencial" : "Para transformar intenção em consistência"),
            plan.Description ?? plan.Headline ?? "Uma rotina mais clara, no seu ritmo.", plan.BadgeText ?? (!free ? "Mais recomendado" : null),
            plan.IsFeatured || plan.Code.Equals(PlanCodes.Ritmo, StringComparison.OrdinalIgnoreCase),
            monthly is null ? null : monthly.Amount.ToString("C", PtBr) + "/mês",
            yearly is null ? null : yearly.Amount.ToString("C", PtBr) + "/ano", saving, benefits,
            free ? "Começar grátis" : "Assinar Ritmo", free ? "/register" : $"/register?intent={Uri.EscapeDataString(plan.Code)}&cycle=Monthly",
            free || monthly is not null || yearly is not null);
    }

    private static string? DescribeFeature(PlanFeatureValue feature) => feature.ValueType.ToLowerInvariant() switch
    {
        "boolean" when feature.BoolValue == true => feature.Name,
        "integer" when feature.IntValue is not null => $"{feature.Name}: {feature.IntValue}",
        "string" when !string.IsNullOrWhiteSpace(feature.StringValue) => $"{feature.Name}: {feature.StringValue}",
        _ => null
    };

    private static IReadOnlyList<PlanComparisonRowViewModel> BuildComparison(IReadOnlyList<PublicPlan> plans)
    {
        string Value(string code, string feature, string unavailable, Func<PlanFeatureValue, string>? format = null) {
            var value = plans.FirstOrDefault(p => p.Code.Equals(code, StringComparison.OrdinalIgnoreCase))?.Features.FirstOrDefault(f => f.Code == feature);
            if (value is null) return unavailable;
            if (format is not null) return format(value);
            return value.IntValue?.ToString(PtBr) ?? (value.BoolValue == true ? "Incluído" : value.StringValue) ?? unavailable;
        }
        return [
            new("Hábitos ativos", Value(PlanCodes.Free, PlanFeatureCodes.ActiveHabitsLimit, "Não informado"), Value(PlanCodes.Ritmo, PlanFeatureCodes.ActiveHabitsLimit, "Não informado")),
            new("Objetivos ativos", Value(PlanCodes.Free, PlanFeatureCodes.ActiveGoalsLimit, "Não informado"), Value(PlanCodes.Ritmo, PlanFeatureCodes.ActiveGoalsLimit, "Não informado")),
            new("Histórico", Value(PlanCodes.Free, PlanFeatureCodes.HistoryDaysLimit, "Não informado", x => x.IntValue is null ? "Não informado" : $"{x.IntValue} dias"), Value(PlanCodes.Ritmo, PlanFeatureCodes.FullHistory, "Não incluído", _ => "Histórico completo")),
            new("Biblioteca", Value(PlanCodes.Free, PlanFeatureCodes.FullHabitLibrary, "Não incluída"), Value(PlanCodes.Ritmo, PlanFeatureCodes.FullHabitLibrary, "Não incluída")),
            new("Relatórios", Value(PlanCodes.Free, PlanFeatureCodes.BasicReports, "Não incluídos", _ => "Resumo semanal básico"), Value(PlanCodes.Ritmo, PlanFeatureCodes.BasicReports, "Não incluídos", _ => "Relatórios disponíveis implementados")),
            new("Exportação", Value(PlanCodes.Free, PlanFeatureCodes.ReportExportCsv, "Não incluída", _ => "Exportação CSV"), Value(PlanCodes.Ritmo, PlanFeatureCodes.ReportExportCsv, "Não incluída", _ => "Exportação CSV")),
            new("Segurança da conta", "Incluída", "Incluída"), new("Central de Privacidade", "Incluída", "Incluída")];
    }

    private static IReadOnlyList<PlanFaqItemViewModel> BuildFaq() => [
        new("Preciso de cartão para começar?", "Não. O plano Gratuito pode ser usado sem cadastrar cartão."),
        new("Posso cancelar?", "Sim. Você pode cancelar a renovação do plano pelos canais disponíveis na sua conta."),
        new("O que acontece se eu cancelar?", "O acesso pago segue as regras do ciclo contratado e depois volta aos limites do Gratuito. Seus dados não são apagados automaticamente."),
        new("Meus dados são apagados?", "Cancelamento e exclusão são ações diferentes. Você pode solicitar exclusão na Central de Privacidade."),
        new("Qual plano é melhor para começar?", "O Gratuito permite conhecer o fluxo essencial. O Ritmo amplia limites e recursos implementados para quem precisa avançar."),
        new("O que o Ritmo libera?", "Os limites e recursos efetivamente disponíveis aparecem na comparação desta página."),
        new("Posso mudar de plano depois?", "Sim, conforme as opções de assinatura disponíveis na sua conta."),
        new("O plano gratuito tem limite?", "Sim. Os limites ajudam a manter uma opção sustentável e aparecem antes da criação da conta."),
        new("O HabitFlow substitui acompanhamento médico ou psicológico?", "Não. O HabitFlow é uma ferramenta de organização e não presta diagnóstico ou tratamento."),
        new("Como funciona minha privacidade?", "Você pode consultar a Política de Privacidade e gerenciar solicitações pela Central de Privacidade.")];
}
