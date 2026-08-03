using HabitFlow.Domain;

namespace HabitFlow.Application;

public sealed record WeeklyReviewHabitResult(Guid HabitId,string Name,int Scheduled,int Completed,int Percentage,string Insight);
public sealed record WeeklyReviewSuggestion(Guid HabitId,string Title,string Description,string Action);
public sealed record RecoverySuggestion(Guid HabitId,string Reason,string Recommendation);
public sealed record WeeklyReviewResult(DateOnly PeriodStart,DateOnly PeriodEnd,int Scheduled,int Completed,int Percentage,string? BestDay,IReadOnlyList<WeeklyReviewHabitResult> Habits,IReadOnlyList<WeeklyReviewSuggestion> Suggestions,IReadOnlyList<RecoverySuggestion> Recovery,bool IsCompleted,string IdempotencyKey);

public sealed class WeeklyReviewService(IHabitRepository habits,IHabitWeekDayRepository weekDays,IHabitCompletionRepository completions,IHabitScheduleExceptionRepository exceptions,IWeeklyReviewRepository reviews,HabitOccurrenceService occurrence,UserTimeZoneService timeZone)
{
    public async Task<WeeklyReviewResult> BuildAsync(Guid clientId,Guid userId,DateOnly periodStart,CancellationToken ct=default)
    {
        if (clientId==Guid.Empty || userId==Guid.Empty) throw new ArgumentException("Conta e pessoa são obrigatórias.");
        var end=periodStart.AddDays(6); var source=await habits.ListActiveAsync(clientId,userId,ct);
        var days=await weekDays.ListByHabitsAsync(source.Select(x=>x.Id),ct);
        var map=days.ToDictionary(x=>x.Key,x=>(IReadOnlySet<int>)x.Value.Select(d=>d.DayOfWeek).ToHashSet());
        var rows=source.Select(h=>new ProgressHabitRow{Id=h.Id,Name=h.Name,Category=h.Category,CreatedAt=h.StartDate?.ToDateTime(TimeOnly.MinValue)??h.CreatedAt,ArchivedAt=h.ArchivedAt,IsArchived=h.IsArchived,FrequencyTypeCode=h.FrequencyType.ToString(),ReminderTime=h.ReminderTime}).ToList();
        var planned=(await occurrence.ListScheduledForPeriodAsync(rows,map,periodStart,end,timeZone.Resolve())).ToList();
        var exceptionList=await exceptions.ListAsync(clientId,userId,periodStart,end,ct);
        planned.RemoveAll(x=>exceptionList.Any(e=>e.HabitId==x.Habit.Id && e.LocalDate==x.Date && (e.Type is HabitScheduleExceptionType.Excused or HabitScheduleExceptionType.Moved)));
        foreach(var e in exceptionList.Where(x=>x.Type==HabitScheduleExceptionType.Added || x.Type==HabitScheduleExceptionType.Moved)) { var date=e.Type==HabitScheduleExceptionType.Moved?e.DestinationDate:e.LocalDate; var row=rows.FirstOrDefault(x=>x.Id==e.HabitId); if(date.HasValue && row is not null && date>=periodStart && date<=end && !planned.Any(x=>x.Habit.Id==row.Id&&x.Date==date)) planned.Add(new(row,date.Value)); }
        var completed=(await completions.ListAsync(clientId,userId,periodStart,end,ct)).ToList();
        var results=source.Select(h=>{var total=planned.Count(x=>x.Habit.Id==h.Id);var done=completed.Count(x=>x.HabitId==h.Id&&planned.Any(p=>p.Habit.Id==h.Id&&p.Date==x.CompletedDate));var pct=total==0?0:(int)Math.Round(done*100d/total);return new WeeklyReviewHabitResult(h.Id,h.Name,total,done,pct,pct>=70?"Seu ritmo esteve consistente.":total>=3?"Um ajuste gentil pode deixar este hábito mais leve.":"Continue observando seu ritmo.");}).Where(x=>x.Scheduled>0).OrderByDescending(x=>x.Percentage).ToList();
        var suggestions=results.Where(x=>x.Scheduled>=3&&x.Percentage<50).Select(x=>new WeeklyReviewSuggestion(x.HabitId,"Ajuste consciente",$"{x.Name} pode ficar mais simples na próxima semana.","Reduzir frequência")).ToList();
        var recovery=results.Where(x=>x.Scheduled>=5&&x.Percentage<40).Select(x=>new RecoverySuggestion(x.HabitId,"A amostra da semana mostra espaço para apoio.","Experimente reduzir a frequência ou escolher um horário mais confortável.")).ToList();
        var best=completed.Where(x=>x.CompletedDate>=periodStart&&x.CompletedDate<=end).GroupBy(x=>x.CompletedDate).OrderByDescending(x=>x.Count()).ThenBy(x=>x.Key).FirstOrDefault()?.Key.ToString("dddd");
        var stored=await reviews.GetAsync(clientId,userId,periodStart,ct); var totalPlanned=planned.Count; var totalDone=completed.Count(x=>planned.Any(p=>p.Habit.Id==x.HabitId&&p.Date==x.CompletedDate));
        return new(periodStart,end,totalPlanned,totalDone,totalPlanned==0?0:(int)Math.Round(totalDone*100d/totalPlanned),best,results,suggestions,recovery,stored?.Status=="Completed",stored?.IdempotencyKey??Guid.NewGuid().ToString("N"));
    }
}

public sealed class CompleteWeeklyReviewUseCase(IWeeklyReviewRepository reviews,TimeProvider clock)
{
    public Task<WeeklyReview> ExecuteAsync(Guid clientId,Guid userId,DateOnly start,string idempotencyKey,CancellationToken ct=default)
    { if(string.IsNullOrWhiteSpace(idempotencyKey)) throw new ArgumentException("Chave de idempotência obrigatória."); var now=clock.GetUtcNow(); return reviews.CompleteAsync(new(Guid.NewGuid(),clientId,userId,start,start.AddDays(6),"Completed",idempotencyKey,1,now,now),ct); }
}
