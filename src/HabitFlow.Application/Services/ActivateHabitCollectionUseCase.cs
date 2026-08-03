using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed record HabitCollectionCustomization(Guid TemplateId, bool Included, string Name,
    HabitFrequencyType FrequencyType, int? TargetPerWeek, IReadOnlyCollection<int> SelectedDays,
    TimeOnly? PreferredTime, string Color, string? Category, DateOnly StartDate);

public sealed record ActivateHabitCollectionCommand(Guid ClientId, Guid UserId, Guid? CollectionId,
    IReadOnlyCollection<HabitCollectionCustomization> Items, Guid? ExistingGoalId, bool CreateGoal,
    string? GoalTitle, GoalTargetType? GoalTargetType, decimal? GoalTargetValue, Guid IdempotencyKey,
    string CorrelationId, string? OnboardingSource, int? ExpectedOnboardingVersion);

public sealed record ActivateHabitCollectionResult(IReadOnlyList<Habit> CreatedHabits,
    IReadOnlyList<Habit> ExistingHabits, UserGoal? Goal, TemplatePlanUsage PlanUsage, string Message);

public sealed class HabitCollectionCustomizationValidator(HabitTemplateCustomizationValidator itemValidator)
{
    public Result Validate(ActivateHabitCollectionCommand command, IReadOnlyList<HabitTemplateCollectionItem> official, DateOnly today)
    {
        if (command.IdempotencyKey == Guid.Empty || string.IsNullOrWhiteSpace(command.CorrelationId))
            return Result.Failure("collection.idempotency_required", "Não foi possível identificar esta solicitação.");
        if (command.Items.Select(x => x.TemplateId).Distinct().Count() != command.Items.Count ||
            command.Items.Any(x => official.All(o => o.TemplateId != x.TemplateId)))
            return Result.Failure("collection.items_invalid", "A coleção contém itens inválidos.");
        if (official.Any(x => x.IsRequired && command.Items.All(c => c.TemplateId != x.TemplateId || !c.Included)))
            return Result.Failure("collection.required_item", "Os hábitos obrigatórios não podem ser removidos.");
        foreach (var item in command.Items.Where(x => x.Included))
        {
            var validation = itemValidator.Validate(new(command.ClientId,command.UserId,item.TemplateId,item.Name,item.FrequencyType,
                item.TargetPerWeek,item.SelectedDays,item.PreferredTime,item.Color,item.Category,null,item.StartDate,
                command.ExistingGoalId,command.CreateGoal,command.GoalTitle,command.GoalTargetType,command.GoalTargetValue,false,
                command.OnboardingSource,command.CollectionId,command.IdempotencyKey,command.CorrelationId),today);
            if (validation.IsFailure) return validation;
        }
        return Result.Success();
    }
}

/// <summary>Persists one validated activation in the caller's transaction; it never begins or commits a transaction.</summary>
public sealed class TemplateActivationService(IHabitRepository habits, IHabitWeekDayRepository weekDays,
    IUserGoalRepository goals, AuditService audit, TimeProvider clock)
{
    public async Task<Habit> ActivateAsync(HabitTemplate template, HabitCollectionCustomization customization,
        ActivateHabitCollectionCommand command, UserGoal? goal, Guid idempotencyKey, CancellationToken ct)
        => await ActivateCoreAsync(template,customization,command,goal,idempotencyKey,null,false,ct);

    private async Task<Habit> ActivateCoreAsync(HabitTemplate template, HabitCollectionCustomization customization,
        ActivateHabitCollectionCommand command, UserGoal? goal, Guid idempotencyKey, string? notes, bool variation, CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var habit = new Habit(Guid.NewGuid(),command.UserId,customization.Name.Trim(),customization.Color,
            string.IsNullOrWhiteSpace(customization.Category) ? template.Category : customization.Category.Trim(),false,null,now,now,
            customization.FrequencyType,customization.TargetPerWeek,customization.PreferredTime,notes?.Trim(),0,command.ClientId,template.Id,
            command.CollectionId,template.ObjectiveId,template.IconCode,template.Difficulty,template.EstimatedTimeMinutes,
            customization.StartDate,template.ContentVersion,variation,idempotencyKey);
        await habits.CreateAsync(habit,ct);
        if (customization.FrequencyType == HabitFrequencyType.CustomWeekly)
            await weekDays.ReplaceAsync(habit.Id,customization.SelectedDays,ct);
        if (goal is not null) await goals.LinkHabitAsync(goal.Id,habit.Id,command.ClientId,command.UserId,ct);
        await audit.LogAsync("habit_collection_item_activated","Item de coleção ativado",
            metadata:new { command.ClientId,command.UserId,command.CollectionId,template.Id,command.CorrelationId },ct:ct);
        return habit;
    }

    public Task<Habit> ActivateAsync(HabitTemplate template, CreateHabitFromTemplateCommand source, UserGoal? goal, CancellationToken ct)
    {
        var collection = new ActivateHabitCollectionCommand(source.ClientId,source.UserId,source.CollectionId,[],
            source.ExistingGoalId,source.CreateGoal,source.GoalTitle,source.GoalTargetType,source.GoalTargetValue,source.IdempotencyKey,
            source.CorrelationId,source.OnboardingSource,null);
        var customization = new HabitCollectionCustomization(source.TemplateId,true,source.Name,source.FrequencyType,
            source.TargetPerWeek,source.SelectedDays,source.PreferredTime,source.Color,source.Category,source.StartDate);
        return ActivateCoreAsync(template,customization,collection,goal,source.IdempotencyKey,source.Notes,source.AllowVariation,ct);
    }
}

public sealed class ActivateHabitCollectionUseCase(IUserRepository users, IHabitTemplateCollectionRepository collections,
    IHabitRepository habits, IUserGoalRepository goals, PlanEntitlementService plans, IUnitOfWork unitOfWork,
    HabitCollectionCustomizationValidator validator, TemplateActivationService activation, NotificationService notifications,
    AuditService audit, UserTimeZoneService dates, TimeProvider clock, ILogger<ActivateHabitCollectionUseCase> logger)
{
    public async Task<Result<ActivateHabitCollectionResult>> ExecuteAsync(ActivateHabitCollectionCommand command, CancellationToken ct=default)
    {
        if (command.ClientId==Guid.Empty || command.UserId==Guid.Empty)
            return Result<ActivateHabitCollectionResult>.Failure("collection.tenant_required","A conta e a pessoa são obrigatórias.");
        try
        {
            await unitOfWork.BeginTransactionAsync(ct);
            var user=await users.GetByIdAsync(command.UserId,ct);
            var collection=command.CollectionId.HasValue ? await collections.GetPublishedAsync(command.CollectionId.Value,ct) : null;
            if (user?.ClientId!=command.ClientId || collection is null) return await Rollback("collection.not_found","Coleção não encontrada.",ct);
            if(!string.Equals(collection.MinimumPlanCode,PlanCodes.Free,StringComparison.OrdinalIgnoreCase) &&
               !await plans.CanUseFullLibraryAsync(command.UserId,ct))
                return await Rollback("collection.plan_required","Esta coleção requer acesso à Biblioteca completa.",ct);
            var official=await collections.ListItemsAsync(collection.Id,ct);
            var validation=validator.Validate(command,official,dates.Today());
            if(validation.IsFailure) return await Rollback(validation.Error.Code,validation.Error.Message,ct);
            var existing=new List<Habit>(); var pending=new List<(HabitTemplateCollectionItem Official,HabitCollectionCustomization Custom)>();
            foreach(var custom in command.Items.Where(x=>x.Included))
            {
                var current=await habits.FindActiveBySourceTemplateAsync(command.ClientId,command.UserId,custom.TemplateId,false,ct);
                if(current is not null) existing.Add(current); else pending.Add((official.Single(x=>x.TemplateId==custom.TemplateId),custom));
            }
            var active=await habits.CountActiveAsync(command.ClientId,command.UserId,ct);
            var limit=await plans.GetIntegerFeatureAsync(command.UserId,PlanFeatureCodes.ActiveHabitsLimit,ct);
            if(limit is >=0 && active+pending.Count>limit) return await Rollback("collection.habit_limit","Seu plano não possui vagas para todos os hábitos selecionados.",ct);
            UserGoal? goal=null;
            if(command.ExistingGoalId.HasValue) goal=await goals.GetAsync(command.ExistingGoalId.Value,command.ClientId,command.UserId,ct);
            if(command.ExistingGoalId.HasValue && goal is null) return await Rollback("collection.goal_forbidden","Objetivo não encontrado nesta conta.",ct);
            if(command.CreateGoal)
            {
                if(string.IsNullOrWhiteSpace(command.GoalTitle)||command.GoalTargetType is null||command.GoalTargetValue is null or <=0)
                    return await Rollback("collection.goal_invalid","Revise os dados do novo objetivo.",ct);
                var now=clock.GetUtcNow().UtcDateTime; var start=pending.FirstOrDefault().Custom?.StartDate??dates.Today();
                goal=new(Guid.NewGuid(),command.ClientId,command.UserId,null,command.GoalTitle.Trim(),null,command.GoalTargetType.Value.ToString(),
                    checked((int)command.GoalTargetValue.Value),0,start,null,"Active","#2563EB",collection.IconCode,now,now,null);
                await goals.CreateAsync(goal,ct);
            }
            var created=new List<Habit>(); var index=0;
            foreach(var item in pending)
                created.Add(await activation.ActivateAsync(item.Official.Template,item.Custom,command,goal,
                    DeriveKey(command.IdempotencyKey,index++),ct));
            await notifications.CreateAsync(command.UserId,"habit_collection_activated","Rotina ativada",
                $"{created.Count} hábitos foram adicionados ao seu dia.","collection",collection.Id,ct);
            await audit.LogAsync("habit_collection_activated","Coleção ativada",metadata:new { command.ClientId,command.UserId,collection.Id,Created=created.Count,Existing=existing.Count,command.CorrelationId },ct:ct);
            await unitOfWork.CommitAsync(ct);
            var final=active+created.Count; var usage=new TemplatePlanUsage(final,limit,limit is null or <0?int.MaxValue:Math.Max(0,limit.Value-final));
            return Result<ActivateHabitCollectionResult>.Success(new(created,existing,goal,usage,"Sua rotina foi ativada com sucesso."));
        }
        catch(Exception ex){await unitOfWork.RollbackAsync(ct);logger.LogError(ex,"Falha ao ativar coleção {CollectionId}; correlation {CorrelationId}",command.CollectionId,command.CorrelationId);return Result<ActivateHabitCollectionResult>.Failure("collection.activation_error","Não foi possível ativar a rotina agora.");}
    }
    private async Task<Result<ActivateHabitCollectionResult>> Rollback(string code,string message,CancellationToken ct){await unitOfWork.RollbackAsync(ct);return Result<ActivateHabitCollectionResult>.Failure(code,message);}
    private static Guid DeriveKey(Guid source,int index){var bytes=source.ToByteArray();BitConverter.GetBytes(index).CopyTo(bytes,12);return new Guid(bytes);}
}
