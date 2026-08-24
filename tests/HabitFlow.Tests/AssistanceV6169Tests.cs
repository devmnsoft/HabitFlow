using HabitFlow.Application;
using Xunit;

namespace HabitFlow.Tests;

public sealed class AssistanceV6169Tests
{
    [Fact] public void Knowledge_answers_common_question_and_searches_reminders()
    {
        var knowledge=new AssistantKnowledgeService();
        Assert.Contains("Criar hábito",knowledge.Match("Como crio um hábito?")!.Title);
        Assert.Contains(knowledge.Search("lembretes"),x=>x.Slug=="lembretes");
    }

    [Fact] public void Safety_blocks_secrets_and_restricts_destructive_actions()
    {
        var safety=new AssistantSafetyPolicy();
        Assert.True(safety.ContainsSensitiveData("token=abc"));
        Assert.Contains("[REMOVIDO]",safety.Sanitize("senha=minha-senha"));
        Assert.True(safety.IsDestructive("quero cancelar assinatura"));
        Assert.True(safety.IsOutOfScope("quero um diagnóstico médico"));
    }

    [Fact] public void Migration_and_routes_enforce_tenant_ownership()
    {
        var root=RepositoryRootLocator.Find();
        var migration=File.ReadAllText(Path.Combine(root,"database/migrations/073_v6169_secure_assistance_support.sql"));
        var repository=File.ReadAllText(Path.Combine(root,"src/HabitFlow.Infrastructure/Repositories/AssistanceRepository.cs"));
        Assert.Contains("client_id uuid not null",migration,StringComparison.OrdinalIgnoreCase);
        Assert.Contains("client_id=@clientId and user_id=@userId",repository,StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("connection string",repository,StringComparison.OrdinalIgnoreCase);
    }

    [Fact] public void Assistant_ui_and_support_fallback_are_present()
    {
        var root=RepositoryRootLocator.Find();
        var assistant=File.ReadAllText(Path.Combine(root,"src/HabitFlow.Web/Views/Assistant/Index.cshtml"));
        var contact=File.ReadAllText(Path.Combine(root,"src/HabitFlow.Web/Views/Shared/Partials/_SupportContact.cshtml"));
        Assert.Contains("Como crio um hábito?",assistant);
        Assert.Contains("mailto:",contact);
        Assert.Contains("WhatsAppUrl",contact);
    }
}
