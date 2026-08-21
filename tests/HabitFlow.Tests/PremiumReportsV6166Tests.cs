using System.Text;
using HabitFlow.Application;
using Xunit;

namespace HabitFlow.Tests;

public sealed class PremiumReportsV6166Tests
{
    [Fact]
    public void Insight_rules_explain_best_habit_risk_streak_and_drop()
    {
        var client=Guid.NewGuid(); var user=Guid.NewGuid();
        var review=Review([
            new(Guid.NewGuid(),"Leitura","Estudo",7,7,100,"Bom ritmo",10,true,7),
            new(Guid.NewGuid(),"Caminhada","Movimento",5,1,20,"Pode ficar leve",20,false,1)]);
        var insights=new HabitInsightService(new FixedTimeProvider()).Build(client,user,review,80);
        Assert.Contains(insights,x=>x.Type==HabitInsightType.BestHabit && x.CalculatedReason.Contains("7 de 7"));
        Assert.Contains(insights,x=>x.Type==HabitInsightType.HabitAtRisk && x.Severity==HabitInsightSeverity.Attention);
        Assert.Contains(insights,x=>x.Type==HabitInsightType.StreakMaintained);
        Assert.Contains(insights,x=>x.Type==HabitInsightType.ConsistencyDropped);
        Assert.All(insights,x=>{Assert.Equal(client,x.ClientId);Assert.Equal(user,x.UserId);Assert.StartsWith("/",x.ActionRoute);});
    }

    [Fact]
    public void Empty_review_produces_no_fake_insights() =>
        Assert.Empty(new HabitInsightService(new FixedTimeProvider()).Build(Guid.NewGuid(),Guid.NewGuid(),Review([])));

    [Fact]
    public void Pdf_and_csv_are_valid_safe_downloads()
    {
        var exporter=new ReportDocumentExporter();
        var pdf=exporter.ToPdf(new(new(2026,8,1),new(2026,8,31),10,8,6,80,"Ritmo consistente"),DateTimeOffset.UtcNow);
        Assert.StartsWith("%PDF-1.4",Encoding.ASCII.GetString(pdf)); Assert.EndsWith("%%EOF",Encoding.ASCII.GetString(pdf));
        var csv=Encoding.UTF8.GetString(exporter.ToCsv(Review([new(Guid.NewGuid(),"=IMPORTXML()","Estudo",2,1,50,"Continue",null,false,1)])));
        Assert.StartsWith("\uFEFFHabito;Categoria",csv); Assert.Contains("'=IMPORTXML()",csv);
    }

    [Fact]
    public void Persistence_and_exports_enforce_tenant_and_backend_plan_gate()
    {
        var root=RepositoryRootLocator.Root;
        var repository=File.ReadAllText(Path.Combine(root,"src/HabitFlow.Infrastructure/Repositories/UserReportRepository.cs"));
        var controller=File.ReadAllText(Path.Combine(root,"src/HabitFlow.Web/Controllers/ReportsController.cs"));
        var migration=File.ReadAllText(Path.Combine(root,"database/migrations/070_v6166_premium_reports.sql"));
        Assert.Contains("client_id=@clientId and user_id=@userId",repository,StringComparison.OrdinalIgnoreCase);
        Assert.Contains("RequirePremiumAsync",controller); Assert.Contains("ReportExportCsv",controller); Assert.Contains("ReportPrint",controller);
        Assert.Contains("unique index",migration,StringComparison.OrdinalIgnoreCase); Assert.Contains("client_id,user_id,report_type,period_start,algorithm_version",migration);
    }

    private static WeeklyReviewResult Review(IReadOnlyList<WeeklyReviewHabitResult> habits)
    {
        var scheduled=habits.Sum(x=>x.Scheduled);var done=habits.Sum(x=>x.Completed);var percentage=scheduled==0?0:(int)Math.Round(done*100d/scheduled);
        return new(new(2026,8,10),new(2026,8,16),scheduled,done,percentage,null,null,habits.FirstOrDefault()?.Name,habits.LastOrDefault()?.Name,null,0,
            habits,[],[],[],[],[],false,"key");
    }
    private sealed class FixedTimeProvider : TimeProvider { public override DateTimeOffset GetUtcNow()=>new(2026,8,21,12,0,0,TimeSpan.Zero); }
}
