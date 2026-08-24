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
 
   
}
