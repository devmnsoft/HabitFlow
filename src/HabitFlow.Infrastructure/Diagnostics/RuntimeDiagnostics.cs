using System.Diagnostics.Metrics;

namespace HabitFlow.Infrastructure;

internal static class RuntimeDiagnostics
{
    private static readonly Meter Meter = new("HabitFlow.Runtime", "6.6.1");
    internal static readonly Counter<long> PlanAccessQueryFailures = Meter.CreateCounter<long>("plan_access_query_failures");
    internal static readonly Counter<long> DapperMaterializationFailures = Meter.CreateCounter<long>("dapper_materialization_failures");
    internal static readonly Counter<long> UnknownPlanCodes = Meter.CreateCounter<long>("unknown_plan_codes");
    internal static readonly Counter<long> InvalidBenefitsStatus = Meter.CreateCounter<long>("invalid_benefits_status");
}
